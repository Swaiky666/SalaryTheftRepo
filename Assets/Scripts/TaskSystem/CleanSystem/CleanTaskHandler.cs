using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

/// <summary>
/// Clean Task Handler
/// </summary>
public class CleanTaskHandler : MonoBehaviour, ITaskHandler
{
    [Header("Clean System Settings")]
    [SerializeField] private SimplifiedCleanSystem cleanSystem;

    [Header("Task Settings")]
    [SerializeField] private int rubbishToCleanForCompletion = 5;
    [SerializeField] private float workProgressPerRubbish = 2f;

    [Header("Repeatable Task Settings")]
    [SerializeField] private bool allowContinuousProgress = true;
    [SerializeField] private float continuousProgressMultiplier = 0.5f;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLog = true;

    private TaskManager taskManager;
    private Dictionary<int, TaskData> activeTasksData = new Dictionary<int, TaskData>();
    private Dictionary<int, int> taskCleanProgress = new Dictionary<int, int>();
    private Dictionary<int, bool> taskCompletionStatus = new Dictionary<int, bool>();

    public void Initialize(TaskManager manager)
    {
        taskManager = manager;
        ValidateComponents();
        BindCleanSystemEvents();

        if (enableDebugLog)
            Debug.Log("[CleanTaskHandler] Clean Task Handler initialized");
    }

    private void ValidateComponents()
    {
        if (cleanSystem == null)
        {
            cleanSystem = FindObjectOfType<SimplifiedCleanSystem>();
        }
        if (rubbishToCleanForCompletion <= 0)
        {
            rubbishToCleanForCompletion = 5;
        }
    }

    private void BindCleanSystemEvents()
    {
        if (cleanSystem != null)
        {
            cleanSystem.OnRubbishCleaned += OnRubbishCleanedCallback;
        }
    }

    private void UnbindCleanSystemEvents()
    {
        if (cleanSystem != null)
        {
            cleanSystem.OnRubbishCleaned -= OnRubbishCleanedCallback;
        }
    }

    public bool CanHandleTask(TaskType taskType)
    {
        return taskType == TaskType.Clean;
    }

    public void StartTask(TaskData taskData, int taskIndex)
    {
        if (taskData == null) return;

        activeTasksData[taskIndex] = taskData;

        if (!taskCleanProgress.ContainsKey(taskIndex))
        {
            taskCleanProgress[taskIndex] = 0;
            taskCompletionStatus[taskIndex] = false;
        }
        else
        {
            if (taskData.isRepeatable)
            {
                taskCompletionStatus[taskIndex] = false;
            }
        }

        if (!taskCompletionStatus[taskIndex] && taskCleanProgress[taskIndex] >= rubbishToCleanForCompletion)
        {
            CompleteTask(taskIndex);
        }

        taskManager?.UpdateTaskUI();
    }

    private void OnRubbishCleanedCallback(int cleanedCount)
    {
        List<int> tasksToUpdate = new List<int>(activeTasksData.Keys);
        bool uiUpdateNeeded = false;

        foreach (int taskIndex in tasksToUpdate)
        {
            TaskData taskData = activeTasksData[taskIndex];

            if (taskData.taskType != TaskType.Clean) continue;

            if (taskCompletionStatus.ContainsKey(taskIndex) && taskCompletionStatus[taskIndex])
            {
                // Task is completed (for non-repeatable) or has been completed at least once (for repeatable)
                if (taskData.isRepeatable && allowContinuousProgress)
                {
                    // For repeatable tasks, add progress directly after the first completion
                    float continuousProgress = workProgressPerRubbish * continuousProgressMultiplier;
                    taskManager?.AddWorkProgress(continuousProgress, taskData.taskName, true);
                    if (enableDebugLog)
                        Debug.Log($"[CleanTaskHandler] Continuous progress added for repeatable task {taskData.taskName}: +{continuousProgress:F2}%");
                }
                continue;
            }

            // Task is not completed (or is a repeatable task being completed for the first time)
            if (taskCleanProgress.ContainsKey(taskIndex))
            {
                taskCleanProgress[taskIndex] = cleanedCount;

                if (cleanedCount >= rubbishToCleanForCompletion)
                {
                    CompleteTask(taskIndex);
                    uiUpdateNeeded = true;
                }
                else
                {
                    uiUpdateNeeded = true;
                }
            }
        }

        if (uiUpdateNeeded)
        {
            taskManager?.UpdateTaskUI();
        }
    }

    private void CompleteTask(int taskIndex)
    {
        if (activeTasksData.ContainsKey(taskIndex) && !taskCompletionStatus[taskIndex])
        {
            TaskData taskData = activeTasksData[taskIndex];

            // Only mark as completed and notify the manager if it's the first time
            if (!taskData.isCompleted || taskData.isRepeatable)
            {
                taskCompletionStatus[taskIndex] = true;
                taskManager?.TaskCompleted(taskData.taskId, taskIndex);

                // Add work progress for the final completion
                // This is a rough way to ensure progress is added if the progress was not added incrementally
                float remainingProgress = (rubbishToCleanForCompletion - (taskCleanProgress.ContainsKey(taskIndex) ? taskCleanProgress[taskIndex] : 0) + 1) * workProgressPerRubbish;
                taskManager?.AddWorkProgress(remainingProgress, taskData.taskName, false);

                if (enableDebugLog)
                    Debug.Log($"[CleanTaskHandler] ✅ Clean task {taskData.taskName} completed. Notifying TaskManager.");
            }
        }
    }

    public void CleanupTasks()
    {
        activeTasksData.Clear();
        taskCleanProgress.Clear();
        taskCompletionStatus.Clear();
        UnbindCleanSystemEvents();
        if (enableDebugLog) Debug.Log("[CleanTaskHandler] Task data cleaned up");
    }

    // --- Data Accessors ---
    public int GetTaskCleanProgress(int taskIndex)
    {
        return taskCleanProgress.ContainsKey(taskIndex) ? taskCleanProgress[taskIndex] : 0;
    }

    public int RubbishToCleanForCompletion => rubbishToCleanForCompletion;
    public float WorkProgressPerRubbish => workProgressPerRubbish;

    // --- Debug Methods ---

    [ContextMenu("Set First Clean Task Progress to Complete")]
    public void SetFirstTaskProgressToComplete()
    {
        foreach (var kvp in activeTasksData)
        {
            if (kvp.Value.taskType == TaskType.Clean)
            {
                ForceCompleteTask(kvp.Key);
                return;
            }
        }
        Debug.LogWarning("[CleanTaskHandler] No active clean tasks available for debug");
    }

    public void ForceCompleteTask(int taskIndex)
    {
        if (!activeTasksData.ContainsKey(taskIndex) || activeTasksData[taskIndex].taskType != TaskType.Clean) return;

        taskCleanProgress[taskIndex] = rubbishToCleanForCompletion;

        if (!taskCompletionStatus.ContainsKey(taskIndex) || !taskCompletionStatus[taskIndex])
        {
            CompleteTask(taskIndex);
        }
        else if (activeTasksData[taskIndex].isRepeatable)
        {
            taskManager?.TaskCompleted(activeTasksData[taskIndex].taskId, taskIndex);
        }

        taskManager?.UpdateTaskUI();
        if (enableDebugLog)
            Debug.Log($"[CleanTaskHandler Debug] Task {activeTasksData[taskIndex].taskName} progress forcefully set to {rubbishToCleanForCompletion}, attempting completion.");
    }

    void OnDestroy()
    {
        UnbindCleanSystemEvents();
    }
}