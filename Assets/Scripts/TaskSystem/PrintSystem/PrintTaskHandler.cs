using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 打印任务处理器
/// 负责处理打印类型的任务，包括生成任务完成器和管理打印机状态
/// </summary>
public class PrintTaskHandler : MonoBehaviour, ITaskHandler
{
    [Header("打印机设置")]
    [SerializeField] private PrinterSystem printerSystem; // 打印机系统引用
    [SerializeField] private string requiredItemTag = "TaskMaterial"; // 需要的物品标签

    [Header("任务完成器设置")]
    [SerializeField] private GameObject taskCompleterPrefab; // 任务完成器预制件
    [SerializeField] private Transform[] taskCompleterSpawnPoints; // 任务完成器生成点

    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true; // 启用调试日志

    // 私有变量
    private TaskManager taskManager; // 任务管理器引用
    private List<GameObject> activeTaskCompleters = new List<GameObject>(); // 当前活跃的任务完成器
    private Dictionary<int, TaskData> activeTasksData = new Dictionary<int, TaskData>(); // 活跃任务数据
    private HashSet<int> usedSpawnPointIndices = new HashSet<int>(); // 已使用的生成点索引
    private Dictionary<int, int> taskToSpawnPointMapping = new Dictionary<int, int>(); // 任务索引到生成点索引的映射
    private Coroutine waitingStateMonitor; // 等待状态监控协程

    /// <summary>
    /// 初始化打印任务处理器
    /// </summary>
    /// <param name="manager">任务管理器引用</param>
    public void Initialize(TaskManager manager)
    {
        taskManager = manager;

        // 验证必要组件
        ValidateComponents();

        if (enableDebugLog)
            Debug.Log("[PrintTaskHandler] 打印任务处理器已初始化");
    }

    /// <summary>
    /// 验证必要组件
    /// </summary>
    private void ValidateComponents()
    {
        if (printerSystem == null)
        {
            Debug.LogWarning("[PrintTaskHandler] 打印机系统引用未设置");
        }

        if (taskCompleterPrefab == null)
        {
            Debug.LogWarning("[PrintTaskHandler] 任务完成器预制件未设置");
        }

        if (taskCompleterSpawnPoints == null || taskCompleterSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[PrintTaskHandler] 任务完成器生成点未设置");
        }
    }

    /// <summary>
    /// 检查是否可以处理指定类型的任务
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <returns>是否可以处理</returns>
    public bool CanHandleTask(TaskType taskType)
    {
        return taskType == TaskType.Print;
    }

    /// <summary>
    /// 启动打印任务
    /// </summary>
    /// <param name="taskData">任务数据</param>
    /// <param name="taskIndex">任务索引</param>
    public void StartTask(TaskData taskData, int taskIndex)
    {
        if (taskData == null)
        {
            Debug.LogError("[PrintTaskHandler] 任务数据为空");
            return;
        }

        // 存储任务数据
        activeTasksData[taskIndex] = taskData;

        // 生成任务完成器
        SpawnTaskCompleter(taskIndex);

        // 检查并更新打印机等待状态
        CheckAndUpdatePrinterWaitingState();

        // 启动等待状态监控
        StartWaitingStateMonitor();

        if (enableDebugLog)
            Debug.Log($"[PrintTaskHandler] 打印任务已启动: {taskData.taskName} (索引: {taskIndex})，当前活跃任务数: {activeTasksData.Count}");
    }

    /// <summary>
    /// 生成任务完成器
    /// </summary>
    /// <param name="taskIndex">任务索引</param>
    private void SpawnTaskCompleter(int taskIndex)
    {
        if (taskCompleterPrefab == null || taskCompleterSpawnPoints == null || taskCompleterSpawnPoints.Length == 0)
        {
            Debug.LogError("[PrintTaskHandler] 无法生成任务完成器：缺少必要组件");
            return;
        }

        if (!activeTasksData.ContainsKey(taskIndex))
        {
            Debug.LogError($"[PrintTaskHandler] 未找到任务索引 {taskIndex} 的数据");
            return;
        }

        TaskData taskData = activeTasksData[taskIndex];

        // 选择一个未使用的生成点
        int selectedSpawnIndex = SelectAvailableSpawnPoint();
        if (selectedSpawnIndex == -1)
        {
            Debug.LogError("[PrintTaskHandler] 没有可用的生成点，无法生成任务完成器");
            return;
        }

        Transform spawnPoint = taskCompleterSpawnPoints[selectedSpawnIndex];

        // 实例化任务完成器
        GameObject taskCompleter = Instantiate(taskCompleterPrefab, spawnPoint.position, spawnPoint.rotation);

        // 配置任务完成器
        TaskCompleter completerComponent = taskCompleter.GetComponent<TaskCompleter>();
        if (completerComponent == null)
        {
            // 如果没有TaskCompleter组件，添加一个
            completerComponent = taskCompleter.AddComponent<TaskCompleter>();
        }

        // 初始化任务完成器，传递显示文本
        completerComponent.Initialize(requiredItemTag, taskIndex, this, taskData.displayText);

        // 记录使用的生成点
        usedSpawnPointIndices.Add(selectedSpawnIndex);

        // 记录任务到生成点的映射
        taskToSpawnPointMapping[taskIndex] = selectedSpawnIndex;

        // 添加到活跃列表
        activeTaskCompleters.Add(taskCompleter);

        if (enableDebugLog)
            Debug.Log($"[PrintTaskHandler] 任务完成器已生成在位置: {spawnPoint.name} (索引: {selectedSpawnIndex})，等待物品标签: {requiredItemTag}，显示文本: {taskData.displayText}");
    }

    /// <summary>
    /// 选择一个可用的生成点
    /// </summary>
    /// <returns>可用生成点的索引，如果没有可用点则返回-1</returns>
    private int SelectAvailableSpawnPoint()
    {
        // 如果没有使用过的生成点，随机选择一个
        if (usedSpawnPointIndices.Count == 0)
        {
            return Random.Range(0, taskCompleterSpawnPoints.Length);
        }

        // 创建可用生成点索引列表
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < taskCompleterSpawnPoints.Length; i++)
        {
            if (!usedSpawnPointIndices.Contains(i))
            {
                availableIndices.Add(i);
            }
        }

        // 如果没有可用的生成点
        if (availableIndices.Count == 0)
        {
            Debug.LogWarning($"[PrintTaskHandler] 所有生成点都已被使用，共有 {taskCompleterSpawnPoints.Length} 个生成点，已使用 {usedSpawnPointIndices.Count} 个");
            return -1;
        }

        // 从可用生成点中随机选择一个
        int randomIndex = Random.Range(0, availableIndices.Count);
        return availableIndices[randomIndex];
    }

    /// <summary>
    /// 启动等待状态监控协程
    /// </summary>
    private void StartWaitingStateMonitor()
    {
        // 如果已经有监控协程在运行，先停止它
        if (waitingStateMonitor != null)
        {
            StopCoroutine(waitingStateMonitor);
        }

        // 只有当有活跃任务时才启动监控
        if (activeTasksData.Count > 0)
        {
            waitingStateMonitor = StartCoroutine(MonitorWaitingState());

            if (enableDebugLog)
                Debug.Log($"[PrintTaskHandler] 已启动等待状态监控协程，活跃任务数: {activeTasksData.Count}");
        }
    }

    /// <summary>
    /// 监控等待状态协程
    /// </summary>
    private System.Collections.IEnumerator MonitorWaitingState()
    {
        if (enableDebugLog)
            Debug.Log("[PrintTaskHandler] 等待状态监控协程开始运行");

        while (activeTasksData.Count > 0)
        {
            yield return new WaitForSeconds(2f); // 每2秒检查一次

            // 如果还有活跃任务，确保等待状态显示
            if (activeTasksData.Count > 0)
            {
                if (enableDebugLog)
                    Debug.Log($"[PrintTaskHandler] 🔍 监控检查：还有 {activeTasksData.Count} 个活跃任务，确保显示等待状态");

                SetPrinterWaitingState(true);
            }
        }

        if (enableDebugLog)
            Debug.Log("[PrintTaskHandler] 监控结束：没有活跃任务");

        waitingStateMonitor = null;
    }

    /// <summary>
    /// 停止等待状态监控
    /// </summary>
    private void StopWaitingStateMonitor()
    {
        if (waitingStateMonitor != null)
        {
            StopCoroutine(waitingStateMonitor);
            waitingStateMonitor = null;

            if (enableDebugLog)
                Debug.Log("[PrintTaskHandler] 已停止等待状态监控协程");
        }
    }

    /// <summary>
    /// 检查并更新打印机等待状态
    /// </summary>
    private void CheckAndUpdatePrinterWaitingState()
    {
        // 只有当没有活跃的打印任务时，才隐藏等待状态
        bool shouldShowWaiting = activeTasksData.Count > 0;

        if (enableDebugLog)
        {
            Debug.Log($"[PrintTaskHandler] 检查打印机等待状态:");
            Debug.Log($"  - 活跃任务数量: {activeTasksData.Count}");
            Debug.Log($"  - 应该显示等待状态: {shouldShowWaiting}");
            Debug.Log($"  - 打印机系统引用: {(printerSystem != null ? "已设置" : "未设置")}");
        }

        SetPrinterWaitingState(shouldShowWaiting);

        if (enableDebugLog)
        {
            if (shouldShowWaiting)
            {
                Debug.Log($"[PrintTaskHandler] ✅ 设置为显示等待状态，还有 {activeTasksData.Count} 个活跃的打印任务");
            }
            else
            {
                Debug.Log("[PrintTaskHandler] ❌ 设置为隐藏等待状态，所有打印任务已完成");
            }
        }
    }

    /// <summary>
    /// 设置打印机等待状态（带防护检查）
    /// </summary>
    /// <param name="isWaiting">是否等待</param>
    private void SetPrinterWaitingState(bool isWaiting)
    {
        if (printerSystem == null)
        {
            Debug.LogWarning("[PrintTaskHandler] 打印机系统引用为空，无法设置等待状态");
            return;
        }

        // 添加防护：如果还有活跃任务，强制显示等待状态
        if (activeTasksData.Count > 0 && !isWaiting)
        {
            Debug.LogWarning($"[PrintTaskHandler] 🚨 防护触发：还有 {activeTasksData.Count} 个活跃任务，强制显示等待状态");
            isWaiting = true;
        }

        if (enableDebugLog)
        {
            Debug.Log($"[PrintTaskHandler] 设置打印机等待状态: {(isWaiting ? "显示" : "隐藏")}");
        }

        printerSystem.SetWaitingForPrintJob(isWaiting);
    }

    /// <summary>
    /// 任务完成回调（由TaskCompleter调用）
    /// </summary>
    /// <param name="taskIndex">任务索引</param>
    /// <param name="completerObject">完成器对象</param>
    public void OnTaskCompleted(int taskIndex, GameObject completerObject)
    {
        if (!activeTasksData.ContainsKey(taskIndex))
        {
            Debug.LogError($"[PrintTaskHandler] 未找到任务索引 {taskIndex} 的数据");
            return;
        }

        TaskData taskData = activeTasksData[taskIndex];

        // 释放生成点
        if (taskToSpawnPointMapping.ContainsKey(taskIndex))
        {
            int spawnPointIndex = taskToSpawnPointMapping[taskIndex];
            usedSpawnPointIndices.Remove(spawnPointIndex);
            taskToSpawnPointMapping.Remove(taskIndex);

            if (enableDebugLog)
                Debug.Log($"[PrintTaskHandler] 已释放生成点索引: {spawnPointIndex}");
        }

        // 移除并销毁任务完成器
        if (activeTaskCompleters.Contains(completerObject))
        {
            activeTaskCompleters.Remove(completerObject);
            Destroy(completerObject);
        }

        // 清理任务数据
        activeTasksData.Remove(taskIndex);

        if (enableDebugLog)
            Debug.Log($"[PrintTaskHandler] ✅ 打印任务完成: {taskData.taskName}，剩余活跃任务: {activeTasksData.Count}");

        // 检查是否还有其他活跃的打印任务
        CheckAndUpdatePrinterWaitingState();

        // 如果没有活跃任务了，停止监控
        if (activeTasksData.Count == 0)
        {
            StopWaitingStateMonitor();
        }

        // 通知任务管理器任务完成
        if (taskManager != null)
        {
            taskManager.OnTaskCompleted(taskIndex);
        }
    }

    /// <summary>
    /// 清理所有活跃的任务
    /// </summary>
    public void CleanupTasks()
    {
        // 停止等待状态监控
        StopWaitingStateMonitor();

        // 销毁所有活跃的任务完成器
        foreach (GameObject completer in activeTaskCompleters)
        {
            if (completer != null)
                Destroy(completer);
        }
        activeTaskCompleters.Clear();

        // 清理任务数据
        activeTasksData.Clear();

        // 清理生成点使用情况
        usedSpawnPointIndices.Clear();
        taskToSpawnPointMapping.Clear();

        // 检查并更新打印机等待状态（此时应该隐藏）
        CheckAndUpdatePrinterWaitingState();

        if (enableDebugLog)
            Debug.Log("[PrintTaskHandler] 已清理所有打印任务和生成点使用情况，停止等待状态监控");
    }

    /// <summary>
    /// 检查打印任务处理器状态（调试用）
    /// </summary>
    [ContextMenu("检查处理器状态")]
    public void CheckHandlerStatus()
    {
        Debug.Log($"[PrintTaskHandler] === 打印任务处理器状态 ===");
        Debug.Log($"活跃任务完成器数量: {activeTaskCompleters.Count}");
        Debug.Log($"活跃任务数据数量: {activeTasksData.Count}");
        Debug.Log($"打印机引用: {(printerSystem != null ? "已设置" : "未设置")}");
        Debug.Log($"任务管理器引用: {(taskManager != null ? "已设置" : "未设置")}");
        Debug.Log($"任务完成器预制件: {(taskCompleterPrefab != null ? taskCompleterPrefab.name : "未设置")}");
        Debug.Log($"生成点数量: {(taskCompleterSpawnPoints != null ? taskCompleterSpawnPoints.Length : 0)}");
        Debug.Log($"已使用生成点数量: {usedSpawnPointIndices.Count}");
        Debug.Log($"任务到生成点映射数量: {taskToSpawnPointMapping.Count}");
        Debug.Log($"等待状态监控协程: {(waitingStateMonitor != null ? "正在运行" : "未运行")}");

        // 显示已使用的生成点
        if (usedSpawnPointIndices.Count > 0)
        {
            string usedIndices = string.Join(", ", usedSpawnPointIndices);
            Debug.Log($"已使用生成点索引: {usedIndices}");
        }

        // 显示活跃任务详情
        foreach (var kvp in activeTasksData)
        {
            TaskData task = kvp.Value;
            int spawnPointIndex = taskToSpawnPointMapping.ContainsKey(kvp.Key) ? taskToSpawnPointMapping[kvp.Key] : -1;
            Debug.Log($"活跃任务 {kvp.Key}: {task.taskName} - 显示文本: {task.displayText} - 生成点索引: {spawnPointIndex}");
        }
    }

    /// <summary>
    /// 检查生成点使用情况（调试用）
    /// </summary>
    [ContextMenu("检查生成点状态")]
    public void CheckSpawnPointStatus()
    {
        Debug.Log($"[PrintTaskHandler] === 生成点状态检查 ===");

        if (taskCompleterSpawnPoints == null || taskCompleterSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[PrintTaskHandler] 没有设置生成点");
            return;
        }

        Debug.Log($"总生成点数量: {taskCompleterSpawnPoints.Length}");
        Debug.Log($"已使用生成点数量: {usedSpawnPointIndices.Count}");
        Debug.Log($"可用生成点数量: {taskCompleterSpawnPoints.Length - usedSpawnPointIndices.Count}");

        for (int i = 0; i < taskCompleterSpawnPoints.Length; i++)
        {
            if (taskCompleterSpawnPoints[i] != null)
            {
                bool isUsed = usedSpawnPointIndices.Contains(i);
                string status = isUsed ? "已使用" : "可用";
                Debug.Log($"生成点 {i}: {taskCompleterSpawnPoints[i].name} - {status}");
            }
            else
            {
                Debug.LogWarning($"生成点 {i}: 引用为空");
            }
        }
    }

    /// <summary>
    /// 手动更新打印机等待状态（调试用）
    /// </summary>
    [ContextMenu("检查打印机等待状态")]
    public void CheckPrinterWaitingStatus()
    {
        Debug.Log($"[PrintTaskHandler] === 打印机等待状态检查 ===");
        Debug.Log($"活跃打印任务数量: {activeTasksData.Count}");
        Debug.Log($"应该显示等待状态: {activeTasksData.Count > 0}");
        Debug.Log($"等待状态监控协程: {(waitingStateMonitor != null ? "正在运行" : "未运行")}");

        // 手动检查并更新状态
        CheckAndUpdatePrinterWaitingState();

        if (printerSystem != null)
        {
            Debug.Log($"打印机系统引用: 已设置");
        }
        else
        {
            Debug.LogWarning("打印机系统引用: 未设置");
        }

        // 显示活跃任务详情
        foreach (var kvp in activeTasksData)
        {
            TaskData task = kvp.Value;
            Debug.Log($"活跃任务 {kvp.Key}: {task.taskName}");
        }
    }

    /// <summary>
    /// 强制启动等待状态监控（调试用）
    /// </summary>
    [ContextMenu("强制启动监控")]
    public void ForceStartMonitor()
    {
        if (activeTasksData.Count > 0)
        {
            StartWaitingStateMonitor();
            Debug.Log("[PrintTaskHandler] 🔄 强制启动等待状态监控");
        }
        else
        {
            Debug.Log("[PrintTaskHandler] ❌ 没有活跃任务，无需启动监控");
        }
    }

    /// <summary>
    /// 手动完成第一个活跃任务（调试用）
    /// </summary>
    [ContextMenu("手动完成第一个任务")]
    public void ManualCompleteFirstTask()
    {
        if (activeTaskCompleters.Count > 0 && activeTaskCompleters[0] != null)
        {
            TaskCompleter completer = activeTaskCompleters[0].GetComponent<TaskCompleter>();
            if (completer != null)
            {
                completer.ManualCompleteTask();
            }
        }
        else
        {
            Debug.Log("[PrintTaskHandler] 没有活跃的任务可以完成");
        }
    }

    void OnDestroy()
    {
        // 停止等待状态监控
        StopWaitingStateMonitor();

        // 确保清理所有任务
        CleanupTasks();
    }
}