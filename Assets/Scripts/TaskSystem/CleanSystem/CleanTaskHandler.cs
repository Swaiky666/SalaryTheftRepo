using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

/// <summary>
/// 清理任务处理器
/// </summary>
public class CleanTaskHandler : MonoBehaviour, ITaskHandler
{
    [Header("清理系统设置")]
    [SerializeField] private SimplifiedCleanSystem cleanSystem;

    [Header("任务设置")]
    [SerializeField] private int rubbishToCleanForCompletion = 5;
    [SerializeField] private float workProgressPerRubbish = 2f;

    [Header("可重复任务设置")]
    [SerializeField] private bool allowContinuousProgress = true;
    [SerializeField] private float continuousProgressMultiplier = 0.5f;

    [Header("调试设置")]
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
            Debug.Log("[CleanTaskHandler] 清理任务处理器已初始化");
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
        foreach (int taskIndex in tasksToUpdate)
        {
            ProcessRubbishCleanedForTask(taskIndex, cleanedCount);
        }
    }

    private void ProcessRubbishCleanedForTask(int taskIndex, int cleanedCount)
    {
        if (!activeTasksData.ContainsKey(taskIndex)) return;

        TaskData taskData = activeTasksData[taskIndex];
        bool wasCompleted = taskCompletionStatus.ContainsKey(taskIndex) && taskCompletionStatus[taskIndex];

        if (taskCleanProgress.ContainsKey(taskIndex))
        {
            taskCleanProgress[taskIndex] += cleanedCount;
        }
        else
        {
            taskCleanProgress[taskIndex] = cleanedCount;
        }

        float progressIncrease = 0f;

        if (wasCompleted && allowContinuousProgress)
        {
            // 任务已完成，按持续进度计算
            progressIncrease = workProgressPerRubbish * cleanedCount * continuousProgressMultiplier;
        }
        else if (!wasCompleted)
        {
            // 任务未完成，计算达到完成条件前的进度
            int progressBefore = taskCleanProgress[taskIndex] - cleanedCount;
            int effectiveCleaned = 0;
            if (progressBefore < rubbishToCleanForCompletion)
            {
                effectiveCleaned = Mathf.Min(cleanedCount, rubbishToCleanForCompletion - progressBefore);
            }
            progressIncrease = workProgressPerRubbish * effectiveCleaned;
        }

        if (progressIncrease > 0)
        {
            taskManager?.AddWorkProgress(progressIncrease, taskData.taskName, wasCompleted);
        }

        if (!wasCompleted && taskCleanProgress[taskIndex] >= rubbishToCleanForCompletion)
        {
            CompleteTask(taskIndex);
        }

        taskManager?.UpdateTaskUI();
    }

    private void CompleteTask(int taskIndex)
    {
        if (!activeTasksData.ContainsKey(taskIndex)) return;

        TaskData taskData = activeTasksData[taskIndex];

        taskCompletionStatus[taskIndex] = true;

        if (enableDebugLog)
            Debug.Log($"[CleanTaskHandler] ✅ 清理任务 {taskData.taskName} (索引: {taskIndex}) 已完成!");

        taskManager?.TaskCompleted(taskData.taskId, taskIndex);
    }

    public void CleanupTasks()
    {
        activeTasksData.Clear();
        taskCleanProgress.Clear();
        taskCompletionStatus.Clear();

        if (enableDebugLog)
            Debug.Log("[CleanTaskHandler] 任务数据已清理");
    }

    // --- 数据访问器 ---

    public int GetTaskCleanProgress(int taskIndex)
    {
        return taskCleanProgress.ContainsKey(taskIndex) ? taskCleanProgress[taskIndex] : 0;
    }

    public int RubbishToCleanForCompletion => rubbishToCleanForCompletion;
    public float WorkProgressPerRubbish => workProgressPerRubbish;

    // --- 调试方法 ---

    [ContextMenu("设置第一个清理任务进度为完成")]
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
        Debug.LogWarning("[CleanTaskHandler] 没有活跃的清理任务可供调试");
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
            Debug.Log($"[CleanTaskHandler Debug] 任务 {activeTasksData[taskIndex].taskName} 进度强制设置为 {rubbishToCleanForCompletion}，并尝试完成");
    }

    void OnDestroy()
    {
        UnbindCleanSystemEvents();
        CleanupTasks();
    }
}