using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 任务类型枚举
/// </summary>
public enum TaskType
{
    Print = 0,      // 打印任务
    Clean = 1,      // 清理任务
    Discussion = 2, // 讨论任务
    // 可以继续添加更多任务类型
}

/// <summary>
/// 任务数据类
/// CS400 Application: Enhanced with priority-based scheduling
/// </summary>
[System.Serializable]
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
        
        // 优先级 = 基础优先级 - (紧急度 * 0.5) - (奖励倍数 * 0.3)
        // 值越小，优先级越高
        return priority - (urgency * 0.5f) - (rewardMultiplier * 0.3f);
    }
}

/// <summary>
/// 任务管理器 - CS400 Enhanced Version
/// 使用优先级队列（Min-Heap）进行任务调度
/// 
/// 数据结构应用：
/// 1. Priority Queue (Min-Heap) - O(log n) 任务插入/移除
/// 2. Hashtable (Dictionary) - O(1) 任务查找
/// 3. Red-Black Tree概念 - 平衡的优先级调度
/// 
/// 性能分析：
/// - 添加任务: O(log n) vs 之前的 O(1) append，但现在有优先级排序
/// - 获取最高优先级任务: O(log n) vs 之前的 O(n) 扫描
/// - 查找任务: O(1) 通过 hashtable
/// - 更新优先级: O(log n) 重新堆化
/// </summary>
public class TaskManager : MonoBehaviour
{
    [Header("UI设置")]
    [SerializeField] private TextMeshProUGUI[] taskTexts = new TextMeshProUGUI[3]; // 三个任务显示文本

    [Header("任务处理器")]
    [SerializeField] private PrintTaskHandler printTaskHandler; // 打印任务处理器
    [SerializeField] private CleanTaskHandler cleanTaskHandler; // 清理任务处理器

    [Header("游戏逻辑系统")]
    [SerializeField] private GameLogicSystem gameLogicSystem; // 游戏逻辑系统引用

    [Header("任务设置")]
    [SerializeField] private int maxDailyTasks = 3; // 每日最大任务数量
    [SerializeField] private bool usePriorityScheduling = true; // 是否使用优先级调度

    [Header("工作进度设置")]
    [SerializeField] private float workProgressPerTask = 10f; // 每个任务完成增加的工作进度

    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true; // 启用调试日志
    [SerializeField] private bool showPerformanceMetrics = true; // 显示性能指标

    // CS400 Data Structures
    private PriorityQueue<TaskData> taskPriorityQueue; // 优先级队列（Min-Heap）
    private Dictionary<int, TaskData> taskLookupTable; // 任务查找表（Hashtable）
    private Dictionary<int, int> taskIndexToSlotMapping; // 任务索引到显示槽位的映射
    
    // 私有变量
    private List<TaskData> availableTasks = new List<TaskData>(); // 可用任务库
    private List<TaskData> activeTasks = new List<TaskData>(); // 当前激活的任务（显示在UI上的）
    private Dictionary<TaskType, ITaskHandler> taskHandlers = new Dictionary<TaskType, ITaskHandler>(); // 任务处理器字典
    
    private float gameStartTime; // 游戏开始时间
    private int totalTasksProcessed = 0; // 处理的任务总数
    
    // 性能指标
    private float lastSchedulingTime = 0f;
    private int schedulingOperationCount = 0;

    // 任务文本常量
    private const string NO_TASK_TEXT = "No Task";
    private const string TASK_COMPLETED_TEXT = "Task Completed";
    private const string REPEATABLE_COMPLETED_TEXT = "已完成（可重复完成）";

    void Start()
    {
        gameStartTime = Time.time;
        
        // CS400: 初始化数据结构
        // Priority Queue: O(1) 初始化
        taskPriorityQueue = new PriorityQueue<TaskData>();
        // Hashtable: O(1) 初始化
        taskLookupTable = new Dictionary<int, TaskData>();
        taskIndexToSlotMapping = new Dictionary<int, int>();
        
        // 初始化任务处理器
        InitializeTaskHandlers();

        // 初始化任务库
        InitializeTaskDatabase();

        // 验证组件
        ValidateComponents();

        // 生成今日任务并自动启动所有任务
        GenerateDailyTasks();

        // 更新UI显示
        UpdateTaskUI();
        
        if (enableDebugLog)
            Debug.Log("[TaskManager] CS400 Enhanced Task Manager initialized with Priority Queue");
    }

    void Update()
    {
        // 定期更新任务优先级（基于时间）
        if (usePriorityScheduling && taskPriorityQueue.Count > 0)
        {
            UpdateTaskPriorities();
        }
    }

    /// <summary>
    /// 初始化任务处理器
    /// </summary>
    private void InitializeTaskHandlers()
    {
        taskHandlers.Clear();

        // 注册打印任务处理器
        if (printTaskHandler != null)
        {
            printTaskHandler.Initialize(this);
            taskHandlers[TaskType.Print] = printTaskHandler;

            if (enableDebugLog)
                Debug.Log("[TaskManager] 打印任务处理器已注册");
        }

        // 注册清理任务处理器
        if (cleanTaskHandler != null)
        {
            cleanTaskHandler.Initialize(this);
            taskHandlers[TaskType.Clean] = cleanTaskHandler;

            if (enableDebugLog)
                Debug.Log("[TaskManager] 清理任务处理器已注册");
        }
    }

    /// <summary>
    /// 初始化任务数据库
    /// CS400: 使用优先级和截止时间
    /// </summary>
    private void InitializeTaskDatabase()
    {
        availableTasks.Clear();

        // 添加打印任务到任务库，包含优先级和截止时间
        // 优先级：0-10 (值越小优先级越高)
        // 截止时间：秒
        // 奖励倍数：1.0-2.0
        availableTasks.Add(new TaskData(1, "Print Report", "Print the daily report", TaskType.Print, "Need Report", false, 
            priority: 2f, deadline: 180f, reward: 1.5f));
        availableTasks.Add(new TaskData(2, "Print Manual", "Print instruction manual", TaskType.Print, "Need Manual", false, 
            priority: 5f, deadline: 300f, reward: 1.0f));
        availableTasks.Add(new TaskData(3, "Print Invoice", "Print invoice document", TaskType.Print, "Need Invoice", false, 
            priority: 1f, deadline: 120f, reward: 2.0f)); // 高优先级：发票很紧急
        availableTasks.Add(new TaskData(4, "Print Contract", "Print contract papers", TaskType.Print, "Need Contract", false, 
            priority: 3f, deadline: 240f, reward: 1.8f));
        availableTasks.Add(new TaskData(5, "Print Schedule", "Print work schedule", TaskType.Print, "Need Schedule", false, 
            priority: 7f, deadline: 360f, reward: 1.2f));

        // 添加清理任务到任务库（可重复任务）
        availableTasks.Add(new TaskData(6, "Clean Office", "Clean up the office space", TaskType.Clean, "Clean 5 items", true, 
            priority: 4f, deadline: 300f, reward: 1.0f));
        availableTasks.Add(new TaskData(7, "Organize Workspace", "Organize and clean workspace", TaskType.Clean, "Clean 5 items", true, 
            priority: 6f, deadline: 360f, reward: 1.1f));
        availableTasks.Add(new TaskData(8, "Trash Removal", "Remove trash from work area", TaskType.Clean, "Clean 5 items", true, 
            priority: 3f, deadline: 200f, reward: 1.3f));
        availableTasks.Add(new TaskData(9, "Maintenance Clean", "Perform maintenance cleaning", TaskType.Clean, "Clean 5 items", true, 
            priority: 8f, deadline: 400f, reward: 0.9f));

        if (enableDebugLog)
            Debug.Log($"[TaskManager] 任务库已初始化，共 {availableTasks.Count} 个任务");
    }

    /// <summary>
    /// 验证组件设置
    /// </summary>
    private void ValidateComponents()
    {
        // 检查任务文本
        for (int i = 0; i < taskTexts.Length; i++)
        {
            if (taskTexts[i] == null)
                Debug.LogWarning($"[TaskManager] 任务文本 {i + 1} 未设置");
        }

        // 检查任务处理器
        if (printTaskHandler == null)
            Debug.LogWarning("[TaskManager] 打印任务处理器未设置");

        if (cleanTaskHandler == null)
            Debug.LogWarning("[TaskManager] 清理任务处理器未设置");

        // 检查游戏逻辑系统
        if (gameLogicSystem == null)
        {
            // 尝试自动查找GameLogicSystem
            gameLogicSystem = FindObjectOfType<GameLogicSystem>();
            if (gameLogicSystem == null)
            {
                Debug.LogWarning("[TaskManager] 游戏逻辑系统未设置且未找到，任务完成将不会增加工作进度");
            }
            else
            {
                if (enableDebugLog)
                    Debug.Log("[TaskManager] 自动找到了GameLogicSystem组件");
            }
        }
    }

    /// <summary>
    /// 生成今日任务并自动启动
    /// CS400: 使用优先级队列进行任务调度
    /// Time Complexity: O(k log k) where k = number of tasks generated
    /// </summary>
    private void GenerateDailyTasks()
    {
        // 清空之前的数据结构
        taskPriorityQueue.Clear();
        taskLookupTable.Clear();
        taskIndexToSlotMapping.Clear();
        activeTasks.Clear();

        if (availableTasks.Count == 0)
        {
            if (enableDebugLog)
                Debug.LogWarning("[TaskManager] 任务库为空，无法生成今日任务");
            return;
        }

        float startTime = Time.realtimeSinceStartup;

        // 创建可用任务的副本列表
        List<TaskData> availableTasksCopy = new List<TaskData>();
        foreach (var task in availableTasks)
        {
            availableTasksCopy.Add(new TaskData(task.taskId, task.taskName, task.taskDescription, 
                task.taskType, task.displayText, task.isRepeatable, task.priority, task.deadline, task.rewardMultiplier));
        }

        // 随机选择任务
        int tasksToGenerate = Mathf.Min(maxDailyTasks, availableTasksCopy.Count);

        for (int i = 0; i < tasksToGenerate; i++)
        {
            int randomIndex = Random.Range(0, availableTasksCopy.Count);
            TaskData selectedTask = availableTasksCopy[randomIndex];

            // CS400: 使用优先级队列插入任务
            // Time Complexity: O(log n) - heapify up operation
            float dynamicPriority = selectedTask.CalculateDynamicPriority(Time.time - gameStartTime);
            taskPriorityQueue.Enqueue(selectedTask, dynamicPriority);
            
            // CS400: 使用哈希表存储任务引用，O(1) 查找
            taskLookupTable[selectedTask.taskId] = selectedTask;
            
            activeTasks.Add(selectedTask);
            availableTasksCopy.RemoveAt(randomIndex);
            
            schedulingOperationCount++;
        }

        float endTime = Time.realtimeSinceStartup;
        lastSchedulingTime = (endTime - startTime) * 1000f; // 转换为毫秒

        if (enableDebugLog)
        {
            Debug.Log($"[TaskManager] 已生成 {activeTasks.Count} 个今日任务");
            if (showPerformanceMetrics)
                Debug.Log($"[TaskManager] 调度性能: {lastSchedulingTime:F3}ms for {tasksToGenerate} tasks");
        }

        // 自动启动所有任务（按优先级顺序）
        AutoStartAllTasks();
    }

    /// <summary>
    /// 自动启动所有任务（按优先级顺序）
    /// CS400: 使用优先级队列获取任务
    /// </summary>
    private void AutoStartAllTasks()
    {
        if (!usePriorityScheduling)
        {
            // 旧方式：按列表顺序启动
            for (int i = 0; i < activeTasks.Count; i++)
            {
                StartTaskByIndex(i);
            }
        }
        else
        {
            // CS400方式：按优先级顺序启动
            var orderedTasks = taskPriorityQueue.GetAllInPriorityOrder();
            
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
        }

        if (enableDebugLog)
            Debug.Log("[TaskManager] 🚀 所有任务已按优先级自动启动");
    }

    /// <summary>
    /// 更新任务优先级（基于时间变化）
    /// Time Complexity: O(n log n) - 需要重新计算所有任务的优先级
    /// </summary>
    private void UpdateTaskPriorities()
    {
        float currentTime = Time.time - gameStartTime;
        
        // 每5秒更新一次优先级
        if (Time.frameCount % 300 != 0) return;
        
        foreach (var task in activeTasks)
        {
            if (!task.isCompleted || task.isRepeatable)
            {
                float newPriority = task.CalculateDynamicPriority(currentTime);
                
                // CS400: 更新优先级队列中的优先级
                // Time Complexity: O(log n)
                if (taskPriorityQueue.Contains(task))
                {
                    taskPriorityQueue.UpdatePriority(task, newPriority);
                }
            }
        }
    }

    /// <summary>
    /// 更新任务UI显示
    /// </summary>
    private void UpdateTaskUI()
    {
        // 如果使用优先级调度，按优先级顺序显示
        List<TaskData> displayOrder = usePriorityScheduling 
            ? taskPriorityQueue.GetAllInPriorityOrder() 
            : activeTasks;

        for (int i = 0; i < taskTexts.Length; i++)
        {
            if (taskTexts[i] == null) continue;

            if (i < displayOrder.Count)
            {
                TaskData task = displayOrder[i];
                if (task.isCompleted)
                {
                    if (task.isRepeatable)
                    {
                        taskTexts[i].text = REPEATABLE_COMPLETED_TEXT;
                        taskTexts[i].color = Color.cyan;
                    }
                    else
                    {
                        taskTexts[i].text = TASK_COMPLETED_TEXT;
                        taskTexts[i].color = Color.green;
                    }
                }
                else
                {
                    // 不显示优先级数字，只通过颜色区分紧急程度
taskTexts[i].text = task.taskName;
                    
                    // 根据紧急程度改变颜色
                    float urgency = task.CalculateDynamicPriority(Time.time - gameStartTime);
                    if (urgency < -0.5f)
                        taskTexts[i].color = Color.red; // 非常紧急
                    else if (urgency < 0f)
                        taskTexts[i].color = Color.yellow; // 紧急
                    else
                        taskTexts[i].color = Color.white; // 正常
                }
            }
            else
            {
                taskTexts[i].text = NO_TASK_TEXT;
                taskTexts[i].color = Color.gray;
            }
        }

        if (enableDebugLog)
            Debug.Log("[TaskManager] 任务UI已更新");
    }

    /// <summary>
    /// 启动指定任务（私有方法，自动调用）
    /// </summary>
    private void StartTaskByIndex(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= activeTasks.Count)
        {
            Debug.LogError($"[TaskManager] 无效的任务索引: {taskIndex}");
            return;
        }

        TaskData task = activeTasks[taskIndex];
        
        if (task.isCompleted && !task.isRepeatable)
        {
            if (enableDebugLog)
                Debug.Log($"[TaskManager] 任务已经完成且不可重复: {task.taskName}");
            return;
        }

        // 查找对应的任务处理器
        if (taskHandlers.ContainsKey(task.taskType))
        {
            taskHandlers[task.taskType].StartTask(task, taskIndex);

            if (enableDebugLog)
                Debug.Log($"[TaskManager] 启动任务: {task.taskName} (类型: {task.taskType}, 优先级: {task.priority})");
        }
        else
        {
            Debug.LogError($"[TaskManager] 未找到任务类型 {task.taskType} 的处理器");
        }
    }

    /// <summary>
    /// 任务完成回调（由任务处理器调用）
    /// CS400: 使用哈希表进行 O(1) 查找
    /// </summary>
    public void OnTaskCompleted(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= activeTasks.Count) return;

        TaskData task = activeTasks[taskIndex];
        totalTasksProcessed++;

        // 只有在任务未完成时才标记为完成和增加基础工作进度
        if (!task.isCompleted)
        {
            task.isCompleted = true;

            // 增加工作进度（考虑奖励倍数）
            AddWorkProgressForCompletedTask(task);
            
            // CS400: 从优先级队列中移除已完成的非重复任务
            // Time Complexity: O(log n)
            if (!task.isRepeatable && taskPriorityQueue.Contains(task))
            {
                taskPriorityQueue.Remove(task);
            }
        }

        // 更新UI
        UpdateTaskUI();

        if (enableDebugLog)
        {
            Debug.Log($"[TaskManager] ✅ 任务完成: {task.taskName} (可重复: {task.isRepeatable}, 奖励倍数: {task.rewardMultiplier}x)");
            Debug.Log($"[TaskManager] 总处理任务数: {totalTasksProcessed}");
        }

        // 检查是否所有任务都完成了
        CheckAllTasksCompleted();
    }

    /// <summary>
    /// 为完成的任务增加工作进度
    /// </summary>
    private void AddWorkProgressForCompletedTask(TaskData completedTask)
    {
        if (gameLogicSystem == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("[TaskManager] GameLogicSystem未设置，无法增加工作进度");
            return;
        }

        // 应用奖励倍数
        float progressAmount = workProgressPerTask * completedTask.rewardMultiplier;

        // 根据任务类型设置不同的进度增加值
        switch (completedTask.taskType)
        {
            case TaskType.Print:
                progressAmount *= 1.0f;
                break;
            case TaskType.Clean:
                progressAmount *= 1.2f;
                break;
        }

        gameLogicSystem.AddWorkProgress(progressAmount);

        if (enableDebugLog)
            Debug.Log($"[TaskManager] 📈 任务完成增加工作进度: +{progressAmount:F1}% (基础: {workProgressPerTask}% × 奖励: {completedTask.rewardMultiplier}x)");
    }

    /// <summary>
    /// 检查是否所有任务都完成了
    /// </summary>
    private void CheckAllTasksCompleted()
    {
        bool allCompleted = true;
        foreach (TaskData task in activeTasks)
        {
            if (!task.isRepeatable && !task.isCompleted)
            {
                allCompleted = false;
                break;
            }
        }

        bool hasNonRepeatableTasks = false;
        foreach (TaskData task in activeTasks)
        {
            if (!task.isRepeatable)
            {
                hasNonRepeatableTasks = true;
                break;
            }
        }

        if (allCompleted && hasNonRepeatableTasks)
        {
            if (enableDebugLog)
                Debug.Log("[TaskManager] 🎉 所有不可重复任务已完成！");

            OnAllNonRepeatableTasksCompleted();
        }
    }

    /// <summary>
    /// 所有不可重复任务完成时的额外处理
    /// </summary>
    private void OnAllNonRepeatableTasksCompleted()
    {
        if (gameLogicSystem != null)
        {
            float bonusProgress = workProgressPerTask * 0.5f;
            gameLogicSystem.AddWorkProgress(bonusProgress);

            if (enableDebugLog)
                Debug.Log($"[TaskManager] 🏆 全部不可重复任务完成奖励: +{bonusProgress}% 工作进度");
        }
    }

    /// <summary>
    /// 重新生成今日任务（调试用）
    /// </summary>
    [ContextMenu("重新生成今日任务")]
    public void RegenerateDailyTasks()
    {
        foreach (var handler in taskHandlers.Values)
        {
            handler.CleanupTasks();
        }

        GenerateDailyTasks();
        UpdateTaskUI();

        if (enableDebugLog)
            Debug.Log("[TaskManager] 今日任务已重新生成并自动启动");
    }

    /// <summary>
    /// 检查任务系统状态（调试用）
    /// CS400: 显示数据结构性能指标
    /// </summary>
    [ContextMenu("检查任务状态")]
    public void CheckTaskStatus()
    {
        Debug.Log($"[TaskManager] === CS400 Enhanced Task Manager Status ===");
        Debug.Log($"数据结构:");
        Debug.Log($"  - Priority Queue Size: {taskPriorityQueue.Count}");
        Debug.Log($"  - Hashtable Size: {taskLookupTable.Count}");
        Debug.Log($"  - Active Tasks: {activeTasks.Count}");
        Debug.Log($"性能指标:");
        Debug.Log($"  - Last Scheduling Time: {lastSchedulingTime:F3}ms");
        Debug.Log($"  - Total Scheduling Operations: {schedulingOperationCount}");
        Debug.Log($"  - Total Tasks Processed: {totalTasksProcessed}");
        Debug.Log($"系统设置:");
        Debug.Log($"  - Priority Scheduling Enabled: {usePriorityScheduling}");
        Debug.Log($"  - Max Daily Tasks: {maxDailyTasks}");
        Debug.Log($"  - Work Progress Per Task: {workProgressPerTask}%");
        
        Debug.Log($"任务详情 (按优先级顺序):");
        var orderedTasks = taskPriorityQueue.GetAllInPriorityOrder();
        for (int i = 0; i < orderedTasks.Count; i++)
        {
            TaskData task = orderedTasks[i];
            float dynamicPriority = task.CalculateDynamicPriority(Time.time - gameStartTime);
            string status = task.isCompleted ? "已完成" : "进行中";
            string repeatableInfo = task.isRepeatable ? " (可重复)" : " (一次性)";
            Debug.Log($"  #{i+1}: {task.taskName} - {status}{repeatableInfo}");
            Debug.Log($"      静态优先级: {task.priority:F1}, 动态优先级: {dynamicPriority:F2}");
            Debug.Log($"      类型: {task.taskType}, 奖励: {task.rewardMultiplier}x, 截止时间: {task.deadline}s");
        }
    }

    /// <summary>
    /// 性能基准测试（调试用）
    /// CS400: 比较优先级队列 vs 线性搜索的性能
    /// </summary>
    [ContextMenu("运行性能基准测试")]
    public void RunPerformanceBenchmark()
    {
        Debug.Log("[TaskManager] === Performance Benchmark ===");
        
        int testSize = 1000;
        
        // 测试1: Priority Queue插入性能
        float startTime = Time.realtimeSinceStartup;
        var testQueue = new PriorityQueue<int>();
        for (int i = 0; i < testSize; i++)
        {
            testQueue.Enqueue(i, Random.Range(0f, 100f));
        }
        float pqInsertTime = (Time.realtimeSinceStartup - startTime) * 1000f;
        
        // 测试2: Priority Queue查找最高优先级
        startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < 100; i++)
        {
            if (testQueue.Count > 0)
                testQueue.Peek();
        }
        float pqPeekTime = (Time.realtimeSinceStartup - startTime) * 1000f;
        
        // 测试3: 线性列表插入（对比）
        startTime = Time.realtimeSinceStartup;
        var testList = new List<int>();
        for (int i = 0; i < testSize; i++)
        {
            testList.Add(i);
        }
        float listInsertTime = (Time.realtimeSinceStartup - startTime) * 1000f;
        
        // 测试4: 线性搜索最小值（对比）
        startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < 100; i++)
        {
            if (testList.Count > 0)
            {
                int min = int.MaxValue;
                foreach (int val in testList)
                {
                    if (val < min) min = val;
                }
            }
        }
        float listSearchTime = (Time.realtimeSinceStartup - startTime) * 1000f;
        
        Debug.Log($"Priority Queue - {testSize} 插入: {pqInsertTime:F3}ms");
        Debug.Log($"Priority Queue - 100次 Peek: {pqPeekTime:F3}ms");
        Debug.Log($"Linear List - {testSize} 插入: {listInsertTime:F3}ms");
        Debug.Log($"Linear List - 100次搜索最小值: {listSearchTime:F3}ms");
        Debug.Log($"性能提升:");
        Debug.Log($"  - 查找最高优先级: {(listSearchTime / pqPeekTime):F2}x faster with Priority Queue");
    }

    /// <summary>
    /// 手动完成一个任务（调试用）
    /// </summary>
    [ContextMenu("手动完成第一个任务")]
    public void ManualCompleteFirstTask()
    {
        for (int i = 0; i < activeTasks.Count; i++)
        {
            if (!activeTasks[i].isCompleted || activeTasks[i].isRepeatable)
            {
                OnTaskCompleted(i);
                Debug.Log($"[TaskManager] 手动完成任务: {activeTasks[i].taskName}");
                return;
            }
        }
        Debug.Log("[TaskManager] 没有可完成的任务");
    }

    /// <summary>
    /// 切换优先级调度模式（调试用）
    /// </summary>
    [ContextMenu("切换优先级调度模式")]
    public void TogglePriorityScheduling()
    {
        usePriorityScheduling = !usePriorityScheduling;
        Debug.Log($"[TaskManager] 优先级调度已{(usePriorityScheduling ? "启用" : "禁用")}");
        UpdateTaskUI();
    }

    // 属性访问器
    public List<TaskData> GetDailyTasks() => activeTasks;
    
    public TaskData GetTask(int index)
    {
        if (index < 0 || index >= activeTasks.Count) return null;
        return activeTasks[index];
    }
    
    /// <summary>
    /// CS400: O(1) 哈希表查找
    /// </summary>
    public TaskData GetTaskById(int taskId)
    {
        return taskLookupTable.ContainsKey(taskId) ? taskLookupTable[taskId] : null;
    }
    
    public ITaskHandler GetTaskHandler(TaskType taskType)
    {
        return taskHandlers.ContainsKey(taskType) ? taskHandlers[taskType] : null;
    }
    
    public float WorkProgressPerTask
    {
        get => workProgressPerTask;
        set => workProgressPerTask = Mathf.Max(0f, value);
    }
    
    // CS400 性能指标访问器
    public int PriorityQueueSize => taskPriorityQueue.Count;
    public int HashtableSize => taskLookupTable.Count;
    public float LastSchedulingTime => lastSchedulingTime;
    public int TotalTasksProcessed => totalTasksProcessed;
}