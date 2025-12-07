using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Random = UnityEngine.Random;

/// <summary>
/// 打印任务处理器
/// </summary>
public class PrintTaskHandler : MonoBehaviour, ITaskHandler
{
    [Header("打印机设置")]
    [SerializeField] private PrinterSystem printerSystem;
    [SerializeField] private string requiredItemTag = "TaskMaterial";

    [Header("任务完成器设置")]
    [SerializeField] private GameObject taskCompleterPrefab;
    [SerializeField] private Transform[] taskCompleterSpawnPoints;

    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true;

    private TaskManager taskManager;
    private List<GameObject> activeTaskCompleters = new List<GameObject>();
    private Dictionary<int, TaskData> activeTasksData = new Dictionary<int, TaskData>();
    private HashSet<int> usedSpawnPointIndices = new HashSet<int>();
    private Dictionary<int, int> taskToSpawnPointMapping = new Dictionary<int, int>();
    private Coroutine waitingStateMonitor;

    public void Initialize(TaskManager manager)
    {
        taskManager = manager;
        ValidateComponents();
        if (enableDebugLog)
            Debug.Log("[PrintTaskHandler] 打印任务处理器已初始化");
    }

    private void ValidateComponents()
    {
        if (printerSystem == null) Debug.LogWarning("[PrintTaskHandler] 打印机系统引用未设置");
        if (taskCompleterPrefab == null) Debug.LogWarning("[PrintTaskHandler] 任务完成器预制件未设置");
        if (taskCompleterSpawnPoints == null || taskCompleterSpawnPoints.Length == 0) Debug.LogWarning("[PrintTaskHandler] 任务完成器生成点未设置");
    }

    public bool CanHandleTask(TaskType taskType)
    {
        return taskType == TaskType.Print;
    }

    public void StartTask(TaskData taskData, int taskIndex)
    {
        if (taskData == null) return;

        activeTasksData[taskIndex] = taskData;
        SpawnTaskCompleter(taskIndex);
        CheckAndUpdatePrinterWaitingState();
        StartWaitingStateMonitor();

        if (enableDebugLog)
            Debug.Log($"[PrintTaskHandler] 打印任务已启动: {taskData.taskName} (索引: {taskIndex})");
    }

    private void SpawnTaskCompleter(int taskIndex)
    {
        if (!activeTasksData.ContainsKey(taskIndex) || taskCompleterPrefab == null || taskCompleterSpawnPoints.Length == 0) return;

        TaskData taskData = activeTasksData[taskIndex];
        int selectedSpawnIndex = SelectAvailableSpawnPoint();
        if (selectedSpawnIndex == -1)
        {
            Debug.LogError("[PrintTaskHandler] 没有可用的生成点，无法生成任务完成器");
            return;
        }

        Transform spawnPoint = taskCompleterSpawnPoints[selectedSpawnIndex];
        GameObject taskCompleter = Instantiate(taskCompleterPrefab, spawnPoint.position, spawnPoint.rotation);

        TaskCompleter completerComponent = taskCompleter.GetComponent<TaskCompleter>() ?? taskCompleter.AddComponent<TaskCompleter>();

        completerComponent.Initialize(requiredItemTag, taskIndex, this, taskData.displayText);
        usedSpawnPointIndices.Add(selectedSpawnIndex);
        taskToSpawnPointMapping[taskIndex] = selectedSpawnIndex;
        activeTaskCompleters.Add(taskCompleter);
    }

    private int SelectAvailableSpawnPoint()
    {
        if (usedSpawnPointIndices.Count < taskCompleterSpawnPoints.Length)
        {
            List<int> availableIndices = new List<int>();
            for (int i = 0; i < taskCompleterSpawnPoints.Length; i++)
            {
                if (!usedSpawnPointIndices.Contains(i))
                {
                    availableIndices.Add(i);
                }
            }
            if (availableIndices.Count > 0)
            {
                return availableIndices[Random.Range(0, availableIndices.Count)];
            }
        }
        return -1;
    }

    private IEnumerator MonitorWaitingState()
    {
        while (activeTasksData.Count > 0)
        {
            yield return new WaitForSeconds(2f);
            if (activeTasksData.Count > 0)
            {
                SetPrinterWaitingState(true);
            }
        }
        waitingStateMonitor = null;
    }

    private void StartWaitingStateMonitor()
    {
        if (waitingStateMonitor != null) StopCoroutine(waitingStateMonitor);
        if (activeTasksData.Count > 0) waitingStateMonitor = StartCoroutine(MonitorWaitingState());
    }

    private void StopWaitingStateMonitor()
    {
        if (waitingStateMonitor != null)
        {
            StopCoroutine(waitingStateMonitor);
            waitingStateMonitor = null;
        }
    }

    private void CheckAndUpdatePrinterWaitingState()
    {
        bool shouldShowWaiting = activeTasksData.Count > 0;
        SetPrinterWaitingState(shouldShowWaiting);
    }

    private void SetPrinterWaitingState(bool isWaiting)
    {
        if (printerSystem == null) return;
        if (activeTasksData.Count > 0 && !isWaiting)
        {
            isWaiting = true;
        }
        printerSystem.SetWaitingForPrintJob(isWaiting);
    }

    /// <summary>
    /// 任务完成回调（由TaskCompleter调用）
    /// </summary>
    public void OnTaskCompleted(int taskIndex, GameObject completerObject)
    {
        if (!activeTasksData.ContainsKey(taskIndex)) return;

        TaskData taskData = activeTasksData[taskIndex];

        // 释放生成点和销毁完成器
        if (taskToSpawnPointMapping.ContainsKey(taskIndex))
        {
            int spawnPointIndex = taskToSpawnPointMapping[taskIndex];
            usedSpawnPointIndices.Remove(spawnPointIndex);
            taskToSpawnPointMapping.Remove(taskIndex);
        }
        if (activeTaskCompleters.Contains(completerObject))
        {
            activeTaskCompleters.Remove(completerObject);
            Destroy(completerObject);
        }

        // 清理任务数据
        activeTasksData.Remove(taskIndex);

        if (enableDebugLog)
            Debug.Log($"[PrintTaskHandler] ✅ 打印任务完成: {taskData.taskName}，剩余活跃任务: {activeTasksData.Count}");

        CheckAndUpdatePrinterWaitingState();
        if (activeTasksData.Count == 0) StopWaitingStateMonitor();

        // 通知任务管理器任务完成 (已修正)
        if (taskManager != null)
        {
            taskManager.TaskCompleted(taskData.taskId, taskIndex);
        }
    }

    public void CleanupTasks()
    {
        StopWaitingStateMonitor();
        foreach (GameObject completer in activeTaskCompleters)
        {
            if (completer != null) Destroy(completer);
        }
        activeTaskCompleters.Clear();
        activeTasksData.Clear();
        usedSpawnPointIndices.Clear();
        taskToSpawnPointMapping.Clear();
        CheckAndUpdatePrinterWaitingState();
    }

    void OnDestroy()
    {
        StopWaitingStateMonitor();
        CleanupTasks();
    }

    // --- 调试方法 ---

    [ContextMenu("检查处理器状态")]
    public void CheckHandlerStatus()
    {
        Debug.Log($"[PrintTaskHandler] === 打印任务处理器状态 ===");
        Debug.Log($"活跃任务完成器数量: {activeTaskCompleters.Count}");
        Debug.Log($"活跃任务数据数量: {activeTasksData.Count}");
        Debug.Log($"已使用生成点数量: {usedSpawnPointIndices.Count}");
        Debug.Log($"等待状态监控协程: {(waitingStateMonitor != null ? "正在运行" : "未运行")}");

        foreach (var kvp in activeTasksData)
        {
            TaskData task = kvp.Value;
            int spawnPointIndex = taskToSpawnPointMapping.ContainsKey(kvp.Key) ? taskToSpawnPointMapping[kvp.Key] : -1;
            Debug.Log($"活跃任务 {kvp.Key}: {task.taskName} - 生成点索引: {spawnPointIndex}");
        }
    }

    [ContextMenu("检查打印机等待状态")]
    public void CheckPrinterWaitingStatus()
    {
        Debug.Log($"[PrintTaskHandler] === 打印机等待状态检查 ===");
        CheckAndUpdatePrinterWaitingState();
        // 假设 PrinterSystem 有一个 IsWaitingForPrintJob 属性
        // if (printerSystem != null) { Debug.Log($"当前打印机等待状态: {printerSystem.IsWaitingForPrintJob}"); }
    }

    [ContextMenu("手动完成第一个任务")]
    public void ManualCompleteFirstTask()
    {
        if (activeTaskCompleters.Count > 0 && activeTaskCompleters[0] != null)
        {
            TaskCompleter completer = activeTaskCompleters[0].GetComponent<TaskCompleter>();
            if (completer != null)
            {
                // FIX: 使用 TaskCompleter 的公共属性 TaskIndex
                int compIndex = completer.TaskIndex;
                OnTaskCompleted(compIndex, activeTaskCompleters[0]);
            }
            else
            {
                Debug.LogWarning("[PrintTaskHandler] 无法获取 TaskCompleter 组件进行手动完成模拟。");
            }
        }
        else
        {
            Debug.Log("[PrintTaskHandler] 没有活跃的打印任务可以完成");
        }
    }
}