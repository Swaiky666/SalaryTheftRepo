using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Random = UnityEngine.Random;

/// <summary>
/// Task type enumeration
/// </summary>
public enum TaskType
{
    Print = 0,      // Print Task
    Clean = 1,      // Clean-up Task
    Discussion = 2, // Discussion Task
}

/// <summary>
/// Task Data Class
/// </summary>
[Serializable]
public class TaskData
{
    public int taskId;              // Task ID
    public string taskName;         // Task Name
    public string taskDescription;  // Task Description
    public string displayText;      // Display text for the task completer
    public TaskType taskType;       // Task Type
    public bool isCompleted;        // Is Completed
    public bool isRepeatable;       // Is Repeatable (e.g., for clean-up tasks)

    // CS400 Enhancement: Priority-based scheduling
    public float priority;          // Task Priority (lower value is higher priority)
    public float deadline;          // Deadline (seconds)
    public float rewardMultiplier;  // Reward Multiplier

    public TaskData(int id, string name, string description, TaskType type, string display = "", bool repeatable = false, float priority = 0f, float deadline = 300f, float reward = 1f)
    {
        taskId = id;
        taskName = name;
        taskDescription = description;
        taskType = type;
        isCompleted = false;
        isRepeatable = repeatable;
        displayText = string.IsNullOrEmpty(display) ? description : display;
        this.priority = priority;
        this.deadline = deadline;
        this.rewardMultiplier = reward;
    }

    /// <summary>
    /// Calculates dynamic priority (considering deadline and reward)
    /// Time Complexity: O(1)
    /// </summary>
    public float CalculateDynamicPriority(float currentTime)
    {
        float timeRemaining = Mathf.Max(0, deadline - currentTime);
        float urgency = deadline > 0 ? (1f - (timeRemaining / deadline)) : 0f;

        // Priority = Base Priority - (Urgency * Weight) - (Reward Multiplier * Weight)
        return priority - (urgency * 5f) - (rewardMultiplier * 1f);
    }
}

/// <summary>
/// Task Manager - CS400 Enhanced Version
/// </summary>
public class TaskManager : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private TextMeshProUGUI[] taskTexts = new TextMeshProUGUI[3];

    [Header("Task Handlers")]
    [SerializeField] private PrintTaskHandler printTaskHandler;
    [SerializeField] private CleanTaskHandler cleanTaskHandler;

    [Header("Game Logic System")]
    [SerializeField] private GameLogicSystem gameLogicSystem;

    [Header("Task Settings")]
    [SerializeField] private int maxDailyTasks = 3;
    [SerializeField] private bool usePriorityScheduling = true;

    [Header("Work Progress Settings")]
    [SerializeField] private float workProgressPerTask = 10f;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showPerformanceMetrics = true;

    // CS400 Data Structures
    private PriorityQueue<TaskData> taskPriorityQueue;
    private Dictionary<int, TaskData> taskLookupTable;
    private Dictionary<int, int> taskIndexToSlotMapping;

    private List<TaskData> availableTasks = new List<TaskData>();
    private List<TaskData> activeTasks = new List<TaskData>();
    private Dictionary<TaskType, ITaskHandler> taskHandlers = new Dictionary<TaskType, ITaskHandler>();

    private float gameStartTime;
    private int totalTasksProcessed = 0;

    private const string NO_TASK_TEXT = "No Task";
    private const string TASK_COMPLETED_TEXT = "Task Completed";
    private const string REPEATABLE_COMPLETED_TEXT = "Completed (Repeatable)";
    private static readonly Color DEFAULT_COLOR = Color.white; // Default color set to white
    private static readonly Color COMPLETED_COLOR = Color.green;
    private static readonly Color REPEATABLE_COLOR = Color.cyan;
    private static readonly Color NO_TASK_COLOR = Color.gray;

    void Start()
    {
        gameStartTime = Time.time;

        taskPriorityQueue = new PriorityQueue<TaskData>();
        taskLookupTable = new Dictionary<int, TaskData>();
        taskIndexToSlotMapping = new Dictionary<int, int>();

        InitializeTaskHandlers();
        InitializeTaskDatabase();
        ValidateComponents();
        GenerateDailyTasks();
        UpdateTaskUI();

        if (enableDebugLog)
            Debug.Log("[TaskManager] CS400 Enhanced Task Manager initialized with Priority Queue");
    }

    void Update()
    {
        if (usePriorityScheduling && taskPriorityQueue.Count > 0)
        {
            UpdateTaskPriorities();
        }
    }

    // --- Initialization and Validation Methods ---

    private void InitializeTaskHandlers()
    {
        taskHandlers.Clear();
        if (printTaskHandler != null)
        {
            printTaskHandler.Initialize(this);
            taskHandlers[TaskType.Print] = printTaskHandler;
        }
        if (cleanTaskHandler != null)
        {
            cleanTaskHandler.Initialize(this);
            taskHandlers[TaskType.Clean] = cleanTaskHandler;
        }
    }

    private void InitializeTaskDatabase()
    {
        availableTasks.Clear();

        // Print Tasks 
        availableTasks.Add(new TaskData(1, "Print Report", "Print the daily report", TaskType.Print, "Need Report", false, priority: 2f, deadline: 180f, reward: 1.5f));
        availableTasks.Add(new TaskData(2, "Print Manual", "Print instruction manual", TaskType.Print, "Need Manual", false, priority: 5f, deadline: 300f, reward: 1.0f));
        availableTasks.Add(new TaskData(3, "Print Invoice", "Print invoice document", TaskType.Print, "Need Invoice", false, priority: 1f, deadline: 120f, reward: 2.0f));
        availableTasks.Add(new TaskData(4, "Print Contract", "Print contract papers", TaskType.Print, "Need Contract", false, priority: 3f, deadline: 240f, reward: 1.8f));
        availableTasks.Add(new TaskData(5, "Print Schedule", "Print work schedule", TaskType.Print, "Need Schedule", false, priority: 7f, deadline: 360f, reward: 1.2f));

        // Clean-up Tasks 
        availableTasks.Add(new TaskData(6, "Clean Office", "Clean up the office space", TaskType.Clean, "Clean 5 items", true, priority: 4f, deadline: 300f, reward: 1.0f));
        availableTasks.Add(new TaskData(7, "Organize Workspace", "Organize and clean workspace", TaskType.Clean, "Clean 5 items", true, priority: 6f, deadline: 360f, reward: 1.1f));
        availableTasks.Add(new TaskData(8, "Trash Removal", "Remove trash from work area", TaskType.Clean, "Clean 5 items", true, priority: 3f, deadline: 200f, reward: 1.3f));
        availableTasks.Add(new TaskData(9, "Maintenance Clean", "Perform maintenance cleaning", TaskType.Clean, "Clean 5 items", true, priority: 8f, deadline: 400f, reward: 0.9f));

        if (enableDebugLog)
            Debug.Log($"[TaskManager] Task database initialized with {availableTasks.Count} tasks");
    }

    private void ValidateComponents()
    {
        for (int i = 0; i < taskTexts.Length; i++)
        {
            if (taskTexts[i] == null)
                Debug.LogWarning($"[TaskManager] Task Text {i + 1} is not set");
        }

        if (gameLogicSystem == null)
        {
            gameLogicSystem = FindObjectOfType<GameLogicSystem>();
        }
    }

    // --- Task Generation Methods ---

    private TaskData CloneTaskData(TaskData original)
    {
        return new TaskData(
            original.taskId,
            original.taskName,
            original.taskDescription,
            original.taskType,
            original.displayText,
            original.isRepeatable,
            original.priority,
            original.deadline,
            original.rewardMultiplier
        );
    }

    /// <summary>
    /// Fixed generation of 1 clean-up task and 2 print tasks.
    /// </summary>
    private void GenerateDailyTasks()
    {
        CleanupAllTasks(false);

        if (availableTasks.Count == 0) return;

        float startTime = Time.realtimeSinceStartup;

        List<TaskData> selectedTasks = new List<TaskData>();

        List<TaskData> cleanTasks = availableTasks.FindAll(t => t.taskType == TaskType.Clean);
        List<TaskData> printTasks = availableTasks.FindAll(t => t.taskType == TaskType.Print);

        // 1. Fixed selection of 1 clean-up task
        if (cleanTasks.Count > 0)
        {
            TaskData selectedCleanTask = cleanTasks[Random.Range(0, cleanTasks.Count)];
            selectedTasks.Add(CloneTaskData(selectedCleanTask));
        }
        else { Debug.LogError("[TaskManager] No available clean-up tasks in the task database!"); }

        // 2. Fixed selection of 2 print tasks
        if (printTasks.Count >= 2)
        {
            List<int> printIndices = new List<int>();
            for (int i = 0; i < printTasks.Count; i++) printIndices.Add(i);

            int firstIndex = printIndices[Random.Range(0, printIndices.Count)];
            selectedTasks.Add(CloneTaskData(printTasks[firstIndex]));
            printIndices.Remove(firstIndex);

            int secondIndex = printIndices[Random.Range(0, printIndices.Count)];
            selectedTasks.Add(CloneTaskData(printTasks[secondIndex]));

        }
        else if (printTasks.Count > 0)
        {
            foreach (var task in printTasks)
            {
                selectedTasks.Add(CloneTaskData(task));
            }
            Debug.LogWarning($"[TaskManager] Only {printTasks.Count} print tasks in the database, less than 2.");
        }
        else { Debug.LogError("[TaskManager] No available print tasks in the task database!"); }

        // 3. Instantiate the selected tasks and add them to the active list
        foreach (var newTask in selectedTasks)
        {
            activeTasks.Add(newTask);
            taskLookupTable[newTask.taskId] = newTask;
        }

        // 4. Task sorting (using dynamic priority)
        List<TaskData> orderedTasks = new List<TaskData>(activeTasks);
        orderedTasks.Sort((a, b) => a.CalculateDynamicPriority(Time.time - gameStartTime).CompareTo(b.CalculateDynamicPriority(Time.time - gameStartTime)));

        // 5. Add tasks to the priority queue
        foreach (var task in orderedTasks)
        {
            float currentPriority = task.CalculateDynamicPriority(Time.time - gameStartTime);
            taskPriorityQueue.Enqueue(task, currentPriority);
        }

        // 6. Start tasks and establish slot mapping
        for (int slot = 0; slot < Mathf.Min(orderedTasks.Count, maxDailyTasks); slot++)
        {
            TaskData task = orderedTasks[slot];
            int taskIndex = activeTasks.IndexOf(task);
            if (taskIndex >= 0)
            {
                taskIndexToSlotMapping[taskIndex] = slot;
                StartTaskByIndex(taskIndex);
            }
        }

        float endTime = Time.realtimeSinceStartup;

        if (enableDebugLog)
            Debug.Log($"[TaskManager] 🚀 Generated and started {activeTasks.Count} tasks (1 Clean-up, 2 Print). Time taken: {(endTime - startTime) * 1000f:F2}ms");
    }

    // --- Task Progress and Callback Methods ---

    private void UpdateTaskPriorities()
    {
        float currentTime = Time.time - gameStartTime;
        if (Time.frameCount % 300 != 0) return;

        foreach (var task in activeTasks)
        {
            if (!task.isCompleted || task.isRepeatable)
            {
                float newPriority = task.CalculateDynamicPriority(currentTime);
                if (taskPriorityQueue.Contains(task))
                {
                    taskPriorityQueue.UpdatePriority(task, newPriority);
                }
            }
        }
        // No need to call UpdateTaskUI here, as it's called after every task completion
    }

    public void UpdateTaskUI()
    {
        List<TaskData> displayOrder = usePriorityScheduling ? taskPriorityQueue.GetAllInPriorityOrder() : activeTasks;

        for (int i = 0; i < taskTexts.Length; i++)
        {
            if (taskTexts[i] == null) continue;

            if (i < displayOrder.Count)
            {
                TaskData task = displayOrder[i];

                if (task.isCompleted)
                {
                    taskTexts[i].text = task.isRepeatable ? REPEATABLE_COMPLETED_TEXT : TASK_COMPLETED_TEXT;
                    taskTexts[i].color = task.isRepeatable ? REPEATABLE_COLOR : COMPLETED_COLOR;
                }
                else
                {
                    taskTexts[i].text = GetTaskDisplayName(task);
                    // *******************************************************************
                    // * Removed logic to change color based on priority, using default color *
                    // *******************************************************************
                    taskTexts[i].color = DEFAULT_COLOR;
                }
            }
            else
            {
                taskTexts[i].text = NO_TASK_TEXT;
                taskTexts[i].color = NO_TASK_COLOR;
            }
        }
    }

    private void StartTaskByIndex(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= activeTasks.Count) return;

        TaskData taskData = activeTasks[taskIndex];

        ITaskHandler handler = GetTaskHandler(taskData.taskType);
        if (handler != null)
        {
            handler.StartTask(taskData, taskIndex);
        }
    }

    /// <summary>
    /// Task Completion Callback
    /// </summary>
    public void TaskCompleted(int taskId, int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= activeTasks.Count) return;

        TaskData completedTask = activeTasks[taskIndex];

        if (!completedTask.isCompleted)
        {
            completedTask.isCompleted = true;

            if (!completedTask.isRepeatable)
            {
                taskPriorityQueue.Remove(completedTask);
            }

            AddWorkProgressForCompletedTask(completedTask);
            UpdateTaskUI();

            if (enableDebugLog)
                Debug.Log($"[TaskManager] 🎉 Task {completedTask.taskName} (ID: {taskId}) marked as completed");

            CheckAllTasksCompleted();
        }
        else if (completedTask.isRepeatable)
        {
            AddWorkProgressForCompletedTask(completedTask);
            if (enableDebugLog)
                Debug.Log($"[TaskManager] 🔁 Repeatable task {completedTask.taskName} completed again, progress increased.");
        }
    }

    private void AddWorkProgressForCompletedTask(TaskData completedTask)
    {
        if (gameLogicSystem == null) return;
        float progressAmount = workProgressPerTask * completedTask.rewardMultiplier;
        gameLogicSystem.AddWorkProgress(progressAmount);
    }

    /// <summary>
    /// Add work progress (from continuous progress of task handler)
    /// </summary>
    public void AddWorkProgress(float amount, string sourceName, bool isContinuous)
    {
        if (gameLogicSystem == null) return;
        gameLogicSystem.AddWorkProgress(amount);

        if (enableDebugLog && isContinuous)
        {
            Debug.Log($"[TaskManager] ➕ Task Handler '{sourceName}' added work progress: +{amount:F2}% (Continuous Progress)");
        }
    }

    private void CheckAllTasksCompleted()
    {
        bool hasNonRepeatableTasks = false;
        bool allNonRepeatableCompleted = true;

        foreach (TaskData task in activeTasks)
        {
            if (!task.isRepeatable)
            {
                hasNonRepeatableTasks = true;
                if (!task.isCompleted)
                {
                    allNonRepeatableCompleted = false;
                    break;
                }
            }
        }

        if (allNonRepeatableCompleted && hasNonRepeatableTasks)
        {
            OnAllNonRepeatableTasksCompleted();
        }
    }

    private void OnAllNonRepeatableTasksCompleted()
    {
        if (gameLogicSystem != null)
        {
            float bonusProgress = workProgressPerTask * 0.5f;
            gameLogicSystem.AddWorkProgress(bonusProgress);
            if (enableDebugLog) Debug.Log($"[TaskManager] 🏆 Bonus for all non-repeatable tasks completed: +{bonusProgress}% Work Progress");
        }
    }

    // --- Data Access and Cleanup Methods ---

    /// <summary>
    /// Generates the name displayed on the task board based on task type and progress.
    /// </summary>
    public string GetTaskDisplayName(TaskData taskData)
    {
        if (taskData == null) return "Missing Task Data";
        int taskIndex = activeTasks.IndexOf(taskData);
        if (taskIndex < 0) return taskData.taskName;

        if (taskData.taskType == TaskType.Clean)
        {
            var handler = GetTaskHandler(TaskType.Clean) as CleanTaskHandler;

            if (handler != null)
            {
                int cleanedCount = handler.GetTaskCleanProgress(taskIndex);
                int totalRequired = handler.RubbishToCleanForCompletion;

                // Clean-up Task: Task Name (Items Cleaned/Total Items)
                return $"{taskData.taskName} ({cleanedCount}/{totalRequired})";
            }
        }

        // Other Tasks: Task Name
        return taskData.taskName;
    }

    private void CleanupAllTasks(bool resetUI = true)
    {
        foreach (var handler in taskHandlers.Values)
        {
            handler.CleanupTasks();
        }

        taskPriorityQueue.Clear();
        taskLookupTable.Clear();
        taskIndexToSlotMapping.Clear();
        activeTasks.Clear();

        if (resetUI)
        {
            for (int i = 0; i < taskTexts.Length; i++)
            {
                if (taskTexts[i] != null)
                {
                    taskTexts[i].text = NO_TASK_TEXT;
                    taskTexts[i].color = NO_TASK_COLOR;
                }
            }
        }
    }

    void OnDestroy()
    {
        CleanupAllTasks();
    }

    // --- Property Accessors ---

    public List<TaskData> GetDailyTasks() => activeTasks;
    public ITaskHandler GetTaskHandler(TaskType taskType)
    {
        return taskHandlers.ContainsKey(taskType) ? taskHandlers[taskType] : null;
    }

    public float WorkProgressPerTask
    {
        get => workProgressPerTask;
        set => workProgressPerTask = Mathf.Max(0.1f, value);
    }

    // --- Debug Methods ---

    [ContextMenu("Regenerate Daily Tasks")]
    public void RegenerateDailyTasks()
    {
        CleanupAllTasks(true);
        GenerateDailyTasks();
        UpdateTaskUI();
        if (enableDebugLog)
            Debug.Log("[TaskManager] ♻️ Tasks regenerated and started");
    }

    [ContextMenu("Manually Complete Highest Priority Task")]
    public void ManualCompleteFirstTask()
    {
        if (taskPriorityQueue.Count == 0)
        {
            Debug.Log("[TaskManager] No tasks in the priority queue.");
            return;
        }

        TaskData taskToComplete = taskPriorityQueue.Peek();
        int taskIndex = activeTasks.IndexOf(taskToComplete);

        if (taskIndex >= 0)
        {
            if (taskToComplete.taskType == TaskType.Clean)
            {
                var handler = GetTaskHandler(TaskType.Clean) as CleanTaskHandler;
                if (handler != null)
                {
                    handler.ForceCompleteTask(taskIndex);
                    Debug.Log($"[TaskManager Debug] Force completed Clean-up task: {taskToComplete.taskName}");
                }
            }
            else
            {
                TaskCompleted(taskToComplete.taskId, taskIndex);
                Debug.Log($"[TaskManager Debug] Force completed task: {taskToComplete.taskName}");
            }
        }
        else
        {
            Debug.LogError($"[TaskManager Debug] Task ID {taskToComplete.taskId} from priority queue not found in active list.");
        }
        UpdateTaskUI();
    }

    [ContextMenu("Toggle Priority Scheduling Mode")]
    public void TogglePriorityScheduling()
    {
        usePriorityScheduling = !usePriorityScheduling;
        if (enableDebugLog)
            Debug.Log($"[TaskManager] Priority scheduling is now {(usePriorityScheduling ? "enabled" : "disabled")}");
        UpdateTaskUI();
    }

    [ContextMenu("Check Manager Status")]
    public void CheckManagerStatus()
    {
        Debug.Log($"[TaskManager] === Manager Status Check === (CS400)");
        Debug.Log($"Current Time: {Time.time - gameStartTime:F1}s");
        Debug.Log($"Work Progress: {(gameLogicSystem != null ? gameLogicSystem.WorkProgress.ToString("F1") + "%" : "N/A")}");
        Debug.Log($"Active Tasks Count: {activeTasks.Count}");
        Debug.Log($"Priority Queue Size: {taskPriorityQueue.Count}");

        Debug.Log("--- Active Task Details (by Priority): ---");

        List<TaskData> orderedTasks = taskPriorityQueue.GetAllInPriorityOrder();
        if (orderedTasks.Count == 0 && activeTasks.Count > 0)
        {
            orderedTasks = activeTasks;
            Debug.Log("(Queue is empty, displaying active tasks list)");
        }

        for (int i = 0; i < orderedTasks.Count; i++)
        {
            TaskData task = orderedTasks[i];
            float currentPriority = task.CalculateDynamicPriority(Time.time - gameStartTime);
            int activeIndex = activeTasks.IndexOf(task);

            string status = task.isCompleted ? (task.isRepeatable ? "Completed (Repeatable)" : "Completed") : "In Progress";

            Debug.Log($"- Slot {i}: ID={task.taskId}, Name='{task.taskName}', Type={task.taskType}, Status={status}, Index={activeIndex}, Priority={currentPriority:F2}");
        }

        Debug.Log("------------------------------------");
    }
}