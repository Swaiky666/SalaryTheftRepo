using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 清理任务处理器
/// 负责处理清理类型的任务，支持可重复完成的任务模式
/// </summary>
public class CleanTaskHandler : MonoBehaviour, ITaskHandler
{
    [Header("清理系统设置")]
    [SerializeField] private SimplifiedCleanSystem cleanSystem; // 清理系统引用

    [Header("任务设置")]
    [SerializeField] private int rubbishToCleanForCompletion = 5; // 完成任务需要清理的垃圾数量
    [SerializeField] private float workProgressPerRubbish = 2f; // 每清理一个垃圾增加的工作进度

    [Header("可重复任务设置")]
    [SerializeField] private bool allowContinuousProgress = true; // 允许任务完成后继续获得进度
    [SerializeField] private float continuousProgressMultiplier = 0.5f; // 持续进度的倍率

    [Header("UI显示设置")]
    [SerializeField] private string completedTaskDisplayText = "已完成（可重复完成）"; // 任务完成后的显示文本

    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true; // 启用调试日志

    // 私有变量
    private TaskManager taskManager; // 任务管理器引用
    private Dictionary<int, TaskData> activeTasksData = new Dictionary<int, TaskData>(); // 活跃任务数据
    private Dictionary<int, int> taskCleanProgress = new Dictionary<int, int>(); // 每个任务的清理进度
    private Dictionary<int, bool> taskCompletionStatus = new Dictionary<int, bool>(); // 任务完成状态
    private int totalRubbishCleaned = 0; // 总清理数量（跨任务）

    /// <summary>
    /// 初始化清理任务处理器
    /// </summary>
    /// <param name="manager">任务管理器引用</param>
    public void Initialize(TaskManager manager)
    {
        taskManager = manager;

        // 验证必要组件
        ValidateComponents();

        // 绑定清理系统事件
        BindCleanSystemEvents();

        if (enableDebugLog)
            Debug.Log("[CleanTaskHandler] 清理任务处理器已初始化");
    }

    /// <summary>
    /// 验证必要组件
    /// </summary>
    private void ValidateComponents()
    {
        if (cleanSystem == null)
        {
            // 尝试自动查找清理系统
            cleanSystem = FindObjectOfType<SimplifiedCleanSystem>();
            if (cleanSystem == null)
            {
                Debug.LogWarning("[CleanTaskHandler] 清理系统引用未设置且未找到");
            }
            else
            {
                if (enableDebugLog)
                    Debug.Log("[CleanTaskHandler] 自动找到了SimplifiedCleanSystem组件");
            }
        }

        if (rubbishToCleanForCompletion <= 0)
        {
            Debug.LogWarning("[CleanTaskHandler] 完成任务需要的垃圾数量应大于0");
            rubbishToCleanForCompletion = 5;
        }

        if (workProgressPerRubbish <= 0)
        {
            Debug.LogWarning("[CleanTaskHandler] 每垃圾工作进度应大于0");
            workProgressPerRubbish = 2f;
        }
    }

    /// <summary>
    /// 绑定清理系统事件
    /// </summary>
    private void BindCleanSystemEvents()
    {
        if (cleanSystem != null)
        {
            cleanSystem.OnRubbishCleaned += OnRubbishCleanedCallback;

            if (enableDebugLog)
                Debug.Log("[CleanTaskHandler] 已绑定清理系统事件");
        }
    }

    /// <summary>
    /// 解绑清理系统事件
    /// </summary>
    private void UnbindCleanSystemEvents()
    {
        if (cleanSystem != null)
        {
            cleanSystem.OnRubbishCleaned -= OnRubbishCleanedCallback;

            if (enableDebugLog)
                Debug.Log("[CleanTaskHandler] 已解绑清理系统事件");
        }
    }

    /// <summary>
    /// 检查是否可以处理指定类型的任务
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <returns>是否可以处理</returns>
    public bool CanHandleTask(TaskType taskType)
    {
        return taskType == TaskType.Clean;
    }

    /// <summary>
    /// 启动清理任务
    /// </summary>
    /// <param name="taskData">任务数据</param>
    /// <param name="taskIndex">任务索引</param>
    public void StartTask(TaskData taskData, int taskIndex)
    {
        if (taskData == null)
        {
            Debug.LogError("[CleanTaskHandler] 任务数据为空");
            return;
        }

        // 存储任务数据
        activeTasksData[taskIndex] = taskData;
        taskCleanProgress[taskIndex] = 0;
        taskCompletionStatus[taskIndex] = false;

        if (enableDebugLog)
            Debug.Log($"[CleanTaskHandler] 清理任务已启动: {taskData.taskName} (索引: {taskIndex})，需要清理 {rubbishToCleanForCompletion} 个垃圾");
    }

    /// <summary>
    /// 垃圾被清理回调
    /// </summary>
    /// <param name="cleanedCount">清理的垃圾数量</param>
    private void OnRubbishCleanedCallback(int cleanedCount)
    {
        totalRubbishCleaned += cleanedCount;

        if (enableDebugLog)
            Debug.Log($"[CleanTaskHandler] 垃圾被清理: +{cleanedCount}，总计: {totalRubbishCleaned}");

        // 处理所有活跃的清理任务
        ProcessRubbishCleanedForAllTasks(cleanedCount);
    }

    /// <summary>
    /// 为所有任务处理垃圾清理
    /// </summary>
    /// <param name="cleanedCount">清理的垃圾数量</param>
    private void ProcessRubbishCleanedForAllTasks(int cleanedCount)
    {
        List<int> tasksToUpdate = new List<int>(activeTasksData.Keys);

        foreach (int taskIndex in tasksToUpdate)
        {
            ProcessRubbishCleanedForTask(taskIndex, cleanedCount);
        }
    }

    /// <summary>
    /// 为单个任务处理垃圾清理
    /// </summary>
    /// <param name="taskIndex">任务索引</param>
    /// <param name="cleanedCount">清理的垃圾数量</param>
    private void ProcessRubbishCleanedForTask(int taskIndex, int cleanedCount)
    {
        if (!activeTasksData.ContainsKey(taskIndex)) return;

        TaskData taskData = activeTasksData[taskIndex];
        bool wasCompleted = taskCompletionStatus[taskIndex];

        // 增加清理进度
        taskCleanProgress[taskIndex] += cleanedCount;

        // 计算工作进度增加值
        float progressIncrease;
        if (wasCompleted && allowContinuousProgress)
        {
            // 任务已完成，使用持续进度倍率
            progressIncrease = workProgressPerRubbish * cleanedCount * continuousProgressMultiplier;
        }
        else
        {
            // 任务未完成，使用正常进度
            progressIncrease = workProgressPerRubbish * cleanedCount;
        }

        // 增加工作进度
        AddWorkProgress(progressIncrease, taskData.taskName, wasCompleted);

        // 检查任务是否达到完成条件
        if (!wasCompleted && taskCleanProgress[taskIndex] >= rubbishToCleanForCompletion)
        {
            CompleteTask(taskIndex);
        }

        if (enableDebugLog)
        {
            string status = wasCompleted ? "（已完成-持续进度）" : "（进行中）";
            Debug.Log($"[CleanTaskHandler] 任务 {taskData.taskName} {status}: 清理进度 {taskCleanProgress[taskIndex]}/{rubbishToCleanForCompletion}，工作进度 +{progressIncrease}%");
        }
    }

    /// <summary>
    /// 完成任务
    /// </summary>
    /// <param name="taskIndex">任务索引</param>
    private void CompleteTask(int taskIndex)
    {
        if (!activeTasksData.ContainsKey(taskIndex)) return;

        TaskData taskData = activeTasksData[taskIndex];
        taskCompletionStatus[taskIndex] = true;

        // 通知任务管理器任务完成
        if (taskManager != null)
        {
            // 修改任务显示文本为可重复完成
            taskData.taskName = completedTaskDisplayText;
            taskManager.OnTaskCompleted(taskIndex);
        }

        if (enableDebugLog)
            Debug.Log($"[CleanTaskHandler] ✅ 清理任务完成: {taskData.taskName}，清理了 {taskCleanProgress[taskIndex]} 个垃圾");
    }

    /// <summary>
    /// 增加工作进度
    /// </summary>
    /// <param name="amount">进度数量</param>
    /// <param name="taskName">任务名称</param>
    /// <param name="isContinuous">是否为持续进度</param>
    private void AddWorkProgress(float amount, string taskName, bool isContinuous)
    {
        // 获取GameLogicSystem
        GameLogicSystem gameLogicSystem = FindObjectOfType<GameLogicSystem>();
        if (gameLogicSystem != null)
        {
            gameLogicSystem.AddWorkProgress(amount);

            if (enableDebugLog)
            {
                string progressType = isContinuous ? "持续" : "正常";
                Debug.Log($"[CleanTaskHandler] 📈 {progressType}工作进度增加: +{amount}% (任务: {taskName})");
            }
        }
        else
        {
            Debug.LogWarning("[CleanTaskHandler] 未找到GameLogicSystem，无法增加工作进度");
        }
    }

    /// <summary>
    /// 清理所有活跃的任务
    /// </summary>
    public void CleanupTasks()
    {
        // 解绑清理系统事件
        UnbindCleanSystemEvents();

        // 清理任务数据
        activeTasksData.Clear();
        taskCleanProgress.Clear();
        taskCompletionStatus.Clear();

        if (enableDebugLog)
            Debug.Log("[CleanTaskHandler] 已清理所有清理任务数据");
    }

    /// <summary>
    /// 获取任务完成状态
    /// </summary>
    /// <param name="taskIndex">任务索引</param>
    /// <returns>是否已完成</returns>
    public bool IsTaskCompleted(int taskIndex)
    {
        return taskCompletionStatus.ContainsKey(taskIndex) && taskCompletionStatus[taskIndex];
    }

    /// <summary>
    /// 获取任务清理进度
    /// </summary>
    /// <param name="taskIndex">任务索引</param>
    /// <returns>清理进度</returns>
    public int GetTaskCleanProgress(int taskIndex)
    {
        return taskCleanProgress.ContainsKey(taskIndex) ? taskCleanProgress[taskIndex] : 0;
    }

    /// <summary>
    /// 获取任务完成进度百分比
    /// </summary>
    /// <param name="taskIndex">任务索引</param>
    /// <returns>完成进度百分比（0-100）</returns>
    public float GetTaskProgressPercentage(int taskIndex)
    {
        if (!taskCleanProgress.ContainsKey(taskIndex)) return 0f;

        return Mathf.Min(100f, (float)taskCleanProgress[taskIndex] / rubbishToCleanForCompletion * 100f);
    }

    /// <summary>
    /// 检查清理任务处理器状态（调试用）
    /// </summary>
    [ContextMenu("检查处理器状态")]
    public void CheckHandlerStatus()
    {
        Debug.Log($"[CleanTaskHandler] === 清理任务处理器状态 ===");
        Debug.Log($"活跃任务数量: {activeTasksData.Count}");
        Debug.Log($"清理系统引用: {(cleanSystem != null ? "已设置" : "未设置")}");
        Debug.Log($"任务管理器引用: {(taskManager != null ? "已设置" : "未设置")}");
        Debug.Log($"完成任务需要垃圾数: {rubbishToCleanForCompletion}");
        Debug.Log($"每垃圾工作进度: {workProgressPerRubbish}%");
        Debug.Log($"允许持续进度: {allowContinuousProgress}");
        Debug.Log($"持续进度倍率: {continuousProgressMultiplier}");
        Debug.Log($"总清理垃圾数: {totalRubbishCleaned}");

        // 显示活跃任务详情
        foreach (var kvp in activeTasksData)
        {
            int taskIndex = kvp.Key;
            TaskData task = kvp.Value;
            int progress = GetTaskCleanProgress(taskIndex);
            bool isCompleted = IsTaskCompleted(taskIndex);
            string status = isCompleted ? "已完成" : "进行中";

            Debug.Log($"任务 {taskIndex}: {task.taskName} - 状态: {status} - 清理进度: {progress}/{rubbishToCleanForCompletion}");
        }
    }

    /// <summary>
    /// 手动清理一个垃圾（调试用）
    /// </summary>
    [ContextMenu("手动清理垃圾")]
    public void ManualCleanRubbish()
    {
        OnRubbishCleanedCallback(1);
        Debug.Log("[CleanTaskHandler] 手动清理了1个垃圾");
    }

    /// <summary>
    /// 设置完成任务需要的垃圾数量
    /// </summary>
    /// <param name="count">垃圾数量</param>
    public void SetRubbishToCleanForCompletion(int count)
    {
        rubbishToCleanForCompletion = Mathf.Max(1, count);
        if (enableDebugLog)
            Debug.Log($"[CleanTaskHandler] 完成任务需要垃圾数设置为: {rubbishToCleanForCompletion}");
    }

    /// <summary>
    /// 设置每垃圾工作进度
    /// </summary>
    /// <param name="progress">工作进度</param>
    public void SetWorkProgressPerRubbish(float progress)
    {
        workProgressPerRubbish = Mathf.Max(0.1f, progress);
        if (enableDebugLog)
            Debug.Log($"[CleanTaskHandler] 每垃圾工作进度设置为: {workProgressPerRubbish}%");
    }

    /// <summary>
    /// 设置是否允许持续进度
    /// </summary>
    /// <param name="allow">是否允许</param>
    public void SetAllowContinuousProgress(bool allow)
    {
        allowContinuousProgress = allow;
        if (enableDebugLog)
            Debug.Log($"[CleanTaskHandler] 允许持续进度设置为: {allowContinuousProgress}");
    }

    void OnDestroy()
    {
        // 解绑事件
        UnbindCleanSystemEvents();

        // 清理任务数据
        CleanupTasks();
    }

    // 属性访问器
    public int RubbishToCleanForCompletion => rubbishToCleanForCompletion;
    public float WorkProgressPerRubbish => workProgressPerRubbish;
    public bool AllowContinuousProgress => allowContinuousProgress;
    public float ContinuousProgressMultiplier => continuousProgressMultiplier;
    public int TotalRubbishCleaned => totalRubbishCleaned;
    public int ActiveTaskCount => activeTasksData.Count;
}