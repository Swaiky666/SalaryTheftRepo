using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Random = UnityEngine.Random;

/// <summary>
/// 任务类型枚举
/// </summary>
public enum TaskType
{
    Print = 0,      // 打印任务
    Clean = 1,      // 清理任务
    Discussion = 2, // 讨论任务
}

/// <summary>
/// 任务数据类
/// </summary>
[Serializable]
public class TaskData
{
    public int taskId;              // 任务ID
    public string taskName;         // 任务名称
    public string taskDescription;  // 任务描述
    public string displayText;      // 任务完成器显示文本
    public TaskType taskType;       // 任务类型
    public bool isCompleted;        // 是否已完成
    public bool isRepeatable;       // 是否可重复（用于清理任务等）

    // CS400 Enhancement: Priority-based scheduling
    public float priority;          // 任务优先级 (值越小优先级越高)
    public float deadline;          // 截止时间（秒）
    public float rewardMultiplier;  // 奖励倍数

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
    /// 计算动态优先级（考虑截止时间和奖励）
    /// Time Complexity: O(1)
    /// </summary>
    public float CalculateDynamicPriority(float currentTime)
    {
        float timeRemaining = Mathf.Max(0, deadline - currentTime);
        float urgency = deadline > 0 ? (1f - (timeRemaining / deadline)) : 0f;

        // 优先级 = 基础优先级 - (紧急度 * 权重) - (奖励倍数 * 权重)
        return priority - (urgency * 5f) - (rewardMultiplier * 1f);
    }
}

/// <summary>
/// 任务管理器 - CS400 Enhanced Version
/// </summary>
public class TaskManager : MonoBehaviour
{
    [Header("UI设置")]
    [SerializeField] private TextMeshProUGUI[] taskTexts = new TextMeshProUGUI[3];

    [Header("任务处理器")]
    [SerializeField] private PrintTaskHandler printTaskHandler;
    [SerializeField] private CleanTaskHandler cleanTaskHandler;

    [Header("游戏逻辑系统")]
    [SerializeField] private GameLogicSystem gameLogicSystem;

    [Header("任务设置")]
    [SerializeField] private int maxDailyTasks = 3;
    [SerializeField] private bool usePriorityScheduling = true;

    [Header("工作进度设置")]
    [SerializeField] private float workProgressPerTask = 10f;

    [Header("调试设置")]
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
    private const string REPEATABLE_COMPLETED_TEXT = "已完成（可重复完成）";
    private static readonly Color DEFAULT_COLOR = Color.white; // 默认颜色设置为白色
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

    // --- 初始化和验证方法 ---

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

        // 打印任务 
        availableTasks.Add(new TaskData(1, "Print Report", "Print the daily report", TaskType.Print, "Need Report", false, priority: 2f, deadline: 180f, reward: 1.5f));
        availableTasks.Add(new TaskData(2, "Print Manual", "Print instruction manual", TaskType.Print, "Need Manual", false, priority: 5f, deadline: 300f, reward: 1.0f));
        availableTasks.Add(new TaskData(3, "Print Invoice", "Print invoice document", TaskType.Print, "Need Invoice", false, priority: 1f, deadline: 120f, reward: 2.0f));
        availableTasks.Add(new TaskData(4, "Print Contract", "Print contract papers", TaskType.Print, "Need Contract", false, priority: 3f, deadline: 240f, reward: 1.8f));
        availableTasks.Add(new TaskData(5, "Print Schedule", "Print work schedule", TaskType.Print, "Need Schedule", false, priority: 7f, deadline: 360f, reward: 1.2f));

        // 清理任务 
        availableTasks.Add(new TaskData(6, "Clean Office", "Clean up the office space", TaskType.Clean, "Clean 5 items", true, priority: 4f, deadline: 300f, reward: 1.0f));
        availableTasks.Add(new TaskData(7, "Organize Workspace", "Organize and clean workspace", TaskType.Clean, "Clean 5 items", true, priority: 6f, deadline: 360f, reward: 1.1f));
        availableTasks.Add(new TaskData(8, "Trash Removal", "Remove trash from work area", TaskType.Clean, "Clean 5 items", true, priority: 3f, deadline: 200f, reward: 1.3f));
        availableTasks.Add(new TaskData(9, "Maintenance Clean", "Perform maintenance cleaning", TaskType.Clean, "Clean 5 items", true, priority: 8f, deadline: 400f, reward: 0.9f));

        if (enableDebugLog)
            Debug.Log($"[TaskManager] 任务库已初始化，共 {availableTasks.Count} 个任务");
    }

    private void ValidateComponents()
    {
        for (int i = 0; i < taskTexts.Length; i++)
        {
            if (taskTexts[i] == null)
                Debug.LogWarning($"[TaskManager] 任务文本 {i + 1} 未设置");
        }

        if (gameLogicSystem == null)
        {
            gameLogicSystem = FindObjectOfType<GameLogicSystem>();
        }
    }

    // --- 任务生成方法 ---

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
    /// 固定生成 1 个清理任务和 2 个打印任务。
    /// </summary>
    private void GenerateDailyTasks()
    {
        CleanupAllTasks(false);

        if (availableTasks.Count == 0) return;

        float startTime = Time.realtimeSinceStartup;

        List<TaskData> selectedTasks = new List<TaskData>();

        List<TaskData> cleanTasks = availableTasks.FindAll(t => t.taskType == TaskType.Clean);
        List<TaskData> printTasks = availableTasks.FindAll(t => t.taskType == TaskType.Print);

        // 1. 固定选取 1 个清理任务
        if (cleanTasks.Count > 0)
        {
            TaskData selectedCleanTask = cleanTasks[Random.Range(0, cleanTasks.Count)];
            selectedTasks.Add(CloneTaskData(selectedCleanTask));
        }
        else { Debug.LogError("[TaskManager] 任务库中没有可用的清理任务！"); }

        // 2. 固定选取 2 个打印任务
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
            Debug.LogWarning($"[TaskManager] 任务库中只有 {printTasks.Count} 个打印任务，不足 2 个。");
        }
        else { Debug.LogError("[TaskManager] 任务库中没有可用的打印任务！"); }

        // 3. 将选取的任务实例化并加入活跃列表
        foreach (var newTask in selectedTasks)
        {
            activeTasks.Add(newTask);
            taskLookupTable[newTask.taskId] = newTask;
        }

        // 4. 任务排序（使用动态优先级）
        List<TaskData> orderedTasks = new List<TaskData>(activeTasks);
        orderedTasks.Sort((a, b) => a.CalculateDynamicPriority(Time.time - gameStartTime).CompareTo(b.CalculateDynamicPriority(Time.time - gameStartTime)));

        // 5. 将任务添加到优先级队列
        foreach (var task in orderedTasks)
        {
            float currentPriority = task.CalculateDynamicPriority(Time.time - gameStartTime);
            taskPriorityQueue.Enqueue(task, currentPriority);
        }

        // 6. 启动任务并建立槽位映射
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
            Debug.Log($"[TaskManager] 🚀 生成并启动了 {activeTasks.Count} 个任务 (1 清理, 2 打印). 耗时: {(endTime - startTime) * 1000f:F2}ms");
    }

    // --- 任务进度和回调方法 ---

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
        // 不需要在这里调用 UpdateTaskUI，因为它会在每次完成任务后调用
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
                    // **********************************************
                    // * 移除根据优先级改变颜色的逻辑，只使用默认颜色 *
                    // **********************************************
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
    /// 任务完成回调
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
                Debug.Log($"[TaskManager] 🎉 任务 {completedTask.taskName} (ID: {taskId}) 已标记完成");

            CheckAllTasksCompleted();
        }
        else if (completedTask.isRepeatable)
        {
            AddWorkProgressForCompletedTask(completedTask);
            if (enableDebugLog)
                Debug.Log($"[TaskManager] 🔁 可重复任务 {completedTask.taskName} 再次完成，进度增加。");
        }
    }

    private void AddWorkProgressForCompletedTask(TaskData completedTask)
    {
        if (gameLogicSystem == null) return;
        float progressAmount = workProgressPerTask * completedTask.rewardMultiplier;
        gameLogicSystem.AddWorkProgress(progressAmount);
    }

    /// <summary>
    /// 增加工作进度 (来自任务处理器持续进度)
    /// </summary>
    public void AddWorkProgress(float amount, string sourceName, bool isContinuous)
    {
        if (gameLogicSystem == null) return;
        gameLogicSystem.AddWorkProgress(amount);

        if (enableDebugLog && isContinuous)
        {
            Debug.Log($"[TaskManager] ➕ 任务处理器 '{sourceName}' 增加工作进度: +{amount:F2}% (持续进度)");
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
            if (enableDebugLog) Debug.Log($"[TaskManager] 🏆 全部不可重复任务完成奖励: +{bonusProgress}% 工作进度");
        }
    }

    // --- 数据访问和清理方法 ---

    /// <summary>
    /// 根据任务类型和进度生成任务板上显示的名称。
    /// </summary>
    public string GetTaskDisplayName(TaskData taskData)
    {
        if (taskData == null) return "任务数据缺失";
        int taskIndex = activeTasks.IndexOf(taskData);
        if (taskIndex < 0) return taskData.taskName;

        if (taskData.taskType == TaskType.Clean)
        {
            var handler = GetTaskHandler(TaskType.Clean) as CleanTaskHandler;

            if (handler != null)
            {
                int cleanedCount = handler.GetTaskCleanProgress(taskIndex);
                int totalRequired = handler.RubbishToCleanForCompletion;

                // 清理任务: 任务名 (已清理垃圾数量/一共垃圾数量)
                return $"{taskData.taskName} ({cleanedCount}/{totalRequired})";
            }
        }

        // 其他任务: 任务名
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

    // --- 属性访问器 ---

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

    // --- 调试方法 ---

    [ContextMenu("重新生成任务")]
    public void RegenerateDailyTasks()
    {
        CleanupAllTasks(true);
        GenerateDailyTasks();
        UpdateTaskUI();
        if (enableDebugLog)
            Debug.Log("[TaskManager] ♻️ 任务已重新生成和启动");
    }

    [ContextMenu("手动完成优先级最高的任务")]
    public void ManualCompleteFirstTask()
    {
        if (taskPriorityQueue.Count == 0)
        {
            Debug.Log("[TaskManager] 优先级队列中没有任务。");
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
                    Debug.Log($"[TaskManager Debug] 强制完成清理任务: {taskToComplete.taskName}");
                }
            }
            else
            {
                TaskCompleted(taskToComplete.taskId, taskIndex);
                Debug.Log($"[TaskManager Debug] 强制完成任务: {taskToComplete.taskName}");
            }
        }
        else
        {
            Debug.LogError($"[TaskManager Debug] 优先级队列中的任务ID {taskToComplete.taskId} 未在活跃列表中找到。");
        }
        UpdateTaskUI();
    }

    [ContextMenu("切换优先级调度模式")]
    public void TogglePriorityScheduling()
    {
        usePriorityScheduling = !usePriorityScheduling;
        if (enableDebugLog)
            Debug.Log($"[TaskManager] 优先级调度已{(usePriorityScheduling ? "启用" : "禁用")}");
        UpdateTaskUI();
    }

    [ContextMenu("检查管理器状态")]
    public void CheckManagerStatus()
    {
        Debug.Log($"[TaskManager] === 管理器状态检查 === (CS400)");
        Debug.Log($"当前时间: {Time.time - gameStartTime:F1}s");
        Debug.Log($"工作进度: {(gameLogicSystem != null ? gameLogicSystem.WorkProgress.ToString("F1") + "%" : "N/A")}");
        Debug.Log($"活跃任务数量: {activeTasks.Count}");
        Debug.Log($"优先级队列大小: {taskPriorityQueue.Count}");

        Debug.Log("--- 活跃任务详情 (按优先级): ---");

        List<TaskData> orderedTasks = taskPriorityQueue.GetAllInPriorityOrder();
        if (orderedTasks.Count == 0 && activeTasks.Count > 0)
        {
            orderedTasks = activeTasks;
            Debug.Log("(队列为空，显示活跃任务列表)");
        }

        for (int i = 0; i < orderedTasks.Count; i++)
        {
            TaskData task = orderedTasks[i];
            float currentPriority = task.CalculateDynamicPriority(Time.time - gameStartTime);
            int activeIndex = activeTasks.IndexOf(task);

            string status = task.isCompleted ? (task.isRepeatable ? "已完成(可重复)" : "已完成") : "进行中";

            Debug.Log($"- Slot {i}: ID={task.taskId}, Name='{task.taskName}', Type={task.taskType}, Status={status}, Index={activeIndex}, Priority={currentPriority:F2}");
        }

        Debug.Log("------------------------------------");
    }
}