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

    public TaskData(int id, string name, string description, TaskType type, string display = "")
    {
        taskId = id;
        taskName = name;
        taskDescription = description;
        taskType = type;
        isCompleted = false;
        displayText = string.IsNullOrEmpty(display) ? description : display;
    }
}

/// <summary>
/// 任务管理器 - 负责分配和记录任务，UI更新
/// </summary>
public class TaskManager : MonoBehaviour
{
    [Header("UI设置")]
    [SerializeField] private TextMeshProUGUI[] taskTexts = new TextMeshProUGUI[3]; // 三个任务显示文本

    [Header("任务处理器")]
    [SerializeField] private PrintTaskHandler printTaskHandler; // 打印任务处理器
    // 未来可以添加更多任务处理器
    // [SerializeField] private CleanTaskHandler cleanTaskHandler;
    // [SerializeField] private DiscussionTaskHandler discussionTaskHandler;

    [Header("游戏逻辑系统")]
    [SerializeField] private GameLogicSystem gameLogicSystem; // 游戏逻辑系统引用

    [Header("任务设置")]
    [SerializeField] private int maxDailyTasks = 3; // 每日最大任务数量

    [Header("工作进度设置")]
    [SerializeField] private float workProgressPerTask = 10f; // 每个任务完成增加的工作进度

    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true; // 启用调试日志

    // 私有变量
    private List<TaskData> availableTasks = new List<TaskData>(); // 可用任务库
    private List<TaskData> dailyTasks = new List<TaskData>(); // 今日任务列表
    private Dictionary<TaskType, ITaskHandler> taskHandlers = new Dictionary<TaskType, ITaskHandler>(); // 任务处理器字典

    // 任务文本常量
    private const string NO_TASK_TEXT = "No Task";
    private const string TASK_COMPLETED_TEXT = "Task Completed";

    void Start()
    {
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

        // 未来可以注册更多处理器
        // if (cleanTaskHandler != null) { ... }
        // if (discussionTaskHandler != null) { ... }
    }

    /// <summary>
    /// 初始化任务数据库
    /// </summary>
    private void InitializeTaskDatabase()
    {
        availableTasks.Clear();

        // 添加打印任务到任务库，包含显示文本
        availableTasks.Add(new TaskData(1, "Print Report", "Print the daily report", TaskType.Print, "Need Report"));
        availableTasks.Add(new TaskData(2, "Print Manual", "Print instruction manual", TaskType.Print, "Need Manual"));
        availableTasks.Add(new TaskData(3, "Print Invoice", "Print invoice document", TaskType.Print, "Need Invoice"));
        availableTasks.Add(new TaskData(4, "Print Contract", "Print contract papers", TaskType.Print, "Need Contract"));
        availableTasks.Add(new TaskData(5, "Print Schedule", "Print work schedule", TaskType.Print, "Need Schedule"));

        // 未来可以添加其他类型任务
        // availableTasks.Add(new TaskData(6, "Clean Office", "Clean the office space", TaskType.Clean, "Clean Area"));
        // availableTasks.Add(new TaskData(7, "Team Meeting", "Attend team discussion", TaskType.Discussion, "Join Meeting"));

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
    /// </summary>
    private void GenerateDailyTasks()
    {
        dailyTasks.Clear();

        if (availableTasks.Count == 0)
        {
            if (enableDebugLog)
                Debug.LogWarning("[TaskManager] 任务库为空，无法生成今日任务");
            return;
        }

        // 创建可用任务的副本列表
        List<TaskData> availableTasksCopy = new List<TaskData>();
        foreach (var task in availableTasks)
        {
            availableTasksCopy.Add(new TaskData(task.taskId, task.taskName, task.taskDescription, task.taskType, task.displayText));
        }

        // 随机选择任务，确保不重复
        int tasksToGenerate = Mathf.Min(maxDailyTasks, availableTasksCopy.Count);

        for (int i = 0; i < tasksToGenerate; i++)
        {
            int randomIndex = Random.Range(0, availableTasksCopy.Count);
            TaskData selectedTask = availableTasksCopy[randomIndex];

            dailyTasks.Add(selectedTask);
            availableTasksCopy.RemoveAt(randomIndex); // 移除已选择的任务，避免重复
        }

        if (enableDebugLog)
            Debug.Log($"[TaskManager] 已生成 {dailyTasks.Count} 个今日任务");

        // 自动启动所有任务
        AutoStartAllTasks();
    }

    /// <summary>
    /// 自动启动所有任务
    /// </summary>
    private void AutoStartAllTasks()
    {
        for (int i = 0; i < dailyTasks.Count; i++)
        {
            StartTask(i);
        }

        if (enableDebugLog)
            Debug.Log("[TaskManager] 🚀 所有任务已自动启动");
    }

    /// <summary>
    /// 更新任务UI显示
    /// </summary>
    private void UpdateTaskUI()
    {
        for (int i = 0; i < taskTexts.Length; i++)
        {
            if (taskTexts[i] == null) continue;

            if (i < dailyTasks.Count)
            {
                TaskData task = dailyTasks[i];
                if (task.isCompleted)
                {
                    taskTexts[i].text = TASK_COMPLETED_TEXT;
                    taskTexts[i].color = Color.green;
                }
                else
                {
                    taskTexts[i].text = task.taskName;
                    taskTexts[i].color = Color.white;
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
    /// <param name="taskIndex">任务索引</param>
    private void StartTask(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= dailyTasks.Count)
        {
            Debug.LogError($"[TaskManager] 无效的任务索引: {taskIndex}");
            return;
        }

        TaskData task = dailyTasks[taskIndex];
        if (task.isCompleted)
        {
            if (enableDebugLog)
                Debug.Log($"[TaskManager] 任务已经完成: {task.taskName}");
            return;
        }

        // 查找对应的任务处理器
        if (taskHandlers.ContainsKey(task.taskType))
        {
            // 委托给对应的任务处理器
            taskHandlers[task.taskType].StartTask(task, taskIndex);

            if (enableDebugLog)
                Debug.Log($"[TaskManager] 启动任务: {task.taskName} (类型: {task.taskType})");
        }
        else
        {
            Debug.LogError($"[TaskManager] 未找到任务类型 {task.taskType} 的处理器");
        }
    }

    /// <summary>
    /// 任务完成回调（由任务处理器调用）
    /// </summary>
    /// <param name="taskIndex">任务索引</param>
    public void OnTaskCompleted(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= dailyTasks.Count) return;

        TaskData task = dailyTasks[taskIndex];
        task.isCompleted = true;

        // 增加工作进度
        AddWorkProgressForCompletedTask(task);

        // 更新UI
        UpdateTaskUI();

        if (enableDebugLog)
            Debug.Log($"[TaskManager] ✅ 任务完成: {task.taskName}");

        // 检查是否所有任务都完成了
        CheckAllTasksCompleted();
    }

    /// <summary>
    /// 为完成的任务增加工作进度
    /// </summary>
    /// <param name="completedTask">完成的任务</param>
    private void AddWorkProgressForCompletedTask(TaskData completedTask)
    {
        if (gameLogicSystem == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("[TaskManager] GameLogicSystem未设置，无法增加工作进度");
            return;
        }

        // 根据任务类型设置不同的进度增加值（可以扩展）
        float progressAmount = workProgressPerTask;

        // 未来可以根据任务类型设置不同的进度值
        switch (completedTask.taskType)
        {
            case TaskType.Print:
                progressAmount = workProgressPerTask;
                break;
                // case TaskType.Clean:
                //     progressAmount = workProgressPerTask * 1.2f; // 清理任务可能更有价值
                //     break;
                // case TaskType.Discussion:
                //     progressAmount = workProgressPerTask * 1.5f; // 讨论任务可能最有价值
                //     break;
        }

        // 增加工作进度
        gameLogicSystem.AddWorkProgress(progressAmount);

        if (enableDebugLog)
            Debug.Log($"[TaskManager] 📈 任务完成增加工作进度: +{progressAmount}% (任务: {completedTask.taskName})");
    }

    /// <summary>
    /// 检查是否所有任务都完成了
    /// </summary>
    private void CheckAllTasksCompleted()
    {
        bool allCompleted = true;
        foreach (TaskData task in dailyTasks)
        {
            if (!task.isCompleted)
            {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted && dailyTasks.Count > 0)
        {
            if (enableDebugLog)
                Debug.Log("[TaskManager] 🎉 所有今日任务已完成！");

            // 可以在这里添加额外的奖励逻辑
            OnAllTasksCompleted();
        }
    }

    /// <summary>
    /// 所有任务完成时的额外处理
    /// </summary>
    private void OnAllTasksCompleted()
    {
        // 可以添加额外的奖励，比如额外的工作进度或薪资奖励
        if (gameLogicSystem != null)
        {
            // 给予完成所有任务的奖励
            float bonusProgress = workProgressPerTask * 0.5f; // 额外50%的进度奖励
            gameLogicSystem.AddWorkProgress(bonusProgress);

            if (enableDebugLog)
                Debug.Log($"[TaskManager] 🏆 全部任务完成奖励: +{bonusProgress}% 工作进度");
        }
    }

    /// <summary>
    /// 重新生成今日任务（调试用）
    /// </summary>
    [ContextMenu("重新生成今日任务")]
    public void RegenerateDailyTasks()
    {
        // 通知所有任务处理器清理
        foreach (var handler in taskHandlers.Values)
        {
            handler.CleanupTasks();
        }

        // 重新生成任务并自动启动
        GenerateDailyTasks();
        UpdateTaskUI();

        if (enableDebugLog)
            Debug.Log("[TaskManager] 今日任务已重新生成并自动启动");
    }

    /// <summary>
    /// 检查任务系统状态（调试用）
    /// </summary>
    [ContextMenu("检查任务状态")]
    public void CheckTaskStatus()
    {
        Debug.Log($"[TaskManager] === 任务系统状态检查 ===");
        Debug.Log($"任务库总数: {availableTasks.Count}");
        Debug.Log($"今日任务数: {dailyTasks.Count}");
        Debug.Log($"注册的任务处理器数量: {taskHandlers.Count}");
        Debug.Log($"GameLogicSystem引用: {(gameLogicSystem != null ? "已设置" : "未设置")}");
        Debug.Log($"每任务进度增加值: {workProgressPerTask}%");

        for (int i = 0; i < dailyTasks.Count; i++)
        {
            TaskData task = dailyTasks[i];
            string status = task.isCompleted ? "已完成" : "进行中";
            Debug.Log($"任务 {i + 1}: {task.taskName} - 状态: {status} - 类型: {task.taskType} - 显示文本: {task.displayText}");
        }
    }

    /// <summary>
    /// 手动完成一个任务（调试用）
    /// </summary>
    [ContextMenu("手动完成第一个任务")]
    public void ManualCompleteFirstTask()
    {
        for (int i = 0; i < dailyTasks.Count; i++)
        {
            if (!dailyTasks[i].isCompleted)
            {
                OnTaskCompleted(i);
                Debug.Log($"[TaskManager] 手动完成任务: {dailyTasks[i].taskName}");
                return;
            }
        }
        Debug.Log("[TaskManager] 没有可完成的任务");
    }

    /// <summary>
    /// 设置每任务工作进度值（调试用）
    /// </summary>
    /// <param name="newValue">新的进度值</param>
    public void SetWorkProgressPerTask(float newValue)
    {
        workProgressPerTask = Mathf.Max(0f, newValue);
        if (enableDebugLog)
            Debug.Log($"[TaskManager] 每任务工作进度设置为: {workProgressPerTask}%");
    }

    /// <summary>
    /// 获取今日任务列表
    /// </summary>
    public List<TaskData> GetDailyTasks() => dailyTasks;

    /// <summary>
    /// 获取指定索引的任务
    /// </summary>
    public TaskData GetTask(int index)
    {
        if (index < 0 || index >= dailyTasks.Count) return null;
        return dailyTasks[index];
    }

    /// <summary>
    /// 获取或设置每任务工作进度值
    /// </summary>
    public float WorkProgressPerTask
    {
        get => workProgressPerTask;
        set => workProgressPerTask = Mathf.Max(0f, value);
    }
}