using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Random = UnityEngine.Random;

/// <summary>
/// Print Task Handler
/// </summary>
public class PrintTaskHandler : MonoBehaviour, ITaskHandler
{
    [Header("Printer Settings")]
    [SerializeField] private PrinterSystem printerSystem;
    [SerializeField] private string requiredItemTag = "TaskMaterial";

    [Header("Task Completer Settings")]
    [SerializeField] private GameObject taskCompleterPrefab;
    [SerializeField] private Transform[] taskCompleterSpawnPoints;

    [Header("Debug Settings")]
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
            Debug.Log("[PrintTaskHandler] Print Task Handler initialized");
    }

    private void ValidateComponents()
    {
        if (printerSystem == null) Debug.LogWarning("[PrintTaskHandler] Printer System reference is not set");
        if (taskCompleterPrefab == null) Debug.LogWarning("[PrintTaskHandler] Task Completer Prefab is not set");
        if (taskCompleterSpawnPoints == null || taskCompleterSpawnPoints.Length == 0) Debug.LogWarning("[PrintTaskHandler] Task Completer Spawn Points are not set");
    }

    public bool CanHandleTask(TaskType taskType)
    {
        return taskType == TaskType.Print;
    }

    /// <summary>
    /// Starts the Print task. It instantiates a TaskCompleter at a spawn point.
    /// </summary>
    public void StartTask(TaskData taskData, int taskIndex)
    {
        if (taskData == null) return;

        activeTasksData[taskIndex] = taskData;
        SpawnTaskCompleter(taskIndex);
        CheckAndUpdatePrinterWaitingState();
        StartWaitingStateMonitor();

        if (enableDebugLog)
            Debug.Log($"[PrintTaskHandler] Print task started: {taskData.taskName} (Index: {taskIndex})");
    }

    private void SpawnTaskCompleter(int taskIndex)
    {
        if (!activeTasksData.ContainsKey(taskIndex) || taskCompleterPrefab == null || taskCompleterSpawnPoints.Length == 0) return;

        TaskData taskData = activeTasksData[taskIndex];
        int selectedSpawnIndex = SelectAvailableSpawnPoint();
        if (selectedSpawnIndex == -1)
        {
            Debug.LogError("[PrintTaskHandler] No available spawn points, cannot spawn Task Completer");
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

    /// <summary>
    /// Callback from TaskCompleter when the task is completed (material is placed).
    /// </summary>
    public void OnTaskCompleted(int taskIndex, GameObject completerObject)
    {
        if (!activeTasksData.ContainsKey(taskIndex)) return;

        TaskData taskData = activeTasksData[taskIndex];

        // Release spawn point and destroy completer
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

        // Cleanup task data and notify TaskManager
        activeTasksData.Remove(taskIndex);
        taskManager?.TaskCompleted(taskData.taskId, taskIndex);
        CheckAndUpdatePrinterWaitingState(); // Update waiting status after task completion

        if (enableDebugLog)
            Debug.Log($"[PrintTaskHandler] ✅ Print task completed: {taskData.taskName}, completer destroyed.");
    }

    private void CheckAndUpdatePrinterWaitingState()
    {
        if (printerSystem == null) return;

        // Check if there are any active tasks (which means TaskCompleters are active)
        bool isWaiting = activeTaskCompleters.Count > 0;

        // Assumes PrinterSystem has a method/property to set its waiting state
        // printerSystem.SetWaitingState(isWaiting);

        if (enableDebugLog)
            Debug.Log($"[PrintTaskHandler] Printer waiting state update: {(isWaiting ? "Waiting for Print" : "Normal")}");
    }

    private void StartWaitingStateMonitor()
    {
        if (waitingStateMonitor == null)
        {
            waitingStateMonitor = StartCoroutine(WaitingStateMonitorCoroutine());
        }
    }

    private void StopWaitingStateMonitor()
    {
        if (waitingStateMonitor != null)
        {
            StopCoroutine(waitingStateMonitor);
            waitingStateMonitor = null;
        }
    }

    private IEnumerator WaitingStateMonitorCoroutine()
    {
        while (true)
        {
            if (activeTaskCompleters.Count > 0)
            {
                // Logic check: if completers are active, printer should be in 'waiting' state
                // Debug.Log($"[PrintTaskHandler] Coroutine: Printer is in waiting state, task completers are ready.");
            }
            else
            {
                // If all completers are gone, the printer is no longer 'waiting' for a print job.
                StopWaitingStateMonitor();
                // Debug.Log($"[PrintTaskHandler] Coroutine: Printer is no longer waiting, stopping waiting state monitor");
                yield break;
            }
            yield return new WaitForSeconds(5.0f); // Check every 5 seconds
        }
    }

    public void CleanupTasks()
    {
        StopWaitingStateMonitor();
        foreach (var completer in activeTaskCompleters)
        {
            if (completer != null)
            {
                Destroy(completer);
            }
        }
        activeTaskCompleters.Clear();
        activeTasksData.Clear();
        usedSpawnPointIndices.Clear();
        taskToSpawnPointMapping.Clear();

        if (enableDebugLog)
            Debug.Log("[PrintTaskHandler] Task completers and data cleaned up");
    }

    // --- Debug Methods ---

    [ContextMenu("Check Handler Status")]
    public void CheckHandlerStatus()
    {
        Debug.Log("[PrintTaskHandler] === Print Task Handler Status Check ===");
        Debug.Log($"Active Completers Count: {activeTaskCompleters.Count}");
        Debug.Log($"Used Spawn Points Count: {usedSpawnPointIndices.Count}");
        Debug.Log($"Waiting State Monitor Coroutine: {(waitingStateMonitor != null ? "Running" : "Not Running")}");

        foreach (var kvp in activeTasksData)
        {
            TaskData task = kvp.Value;
            int spawnPointIndex = taskToSpawnPointMapping.ContainsKey(kvp.Key) ? taskToSpawnPointMapping[kvp.Key] : -1;
            Debug.Log($"Active Task {kvp.Key}: {task.taskName} - Spawn Point Index: {spawnPointIndex}");
        }
    }

    [ContextMenu("Check Printer Waiting Status")]
    public void CheckPrinterWaitingStatus()
    {
        Debug.Log("[PrintTaskHandler] === Printer Waiting Status Check ===");
        CheckAndUpdatePrinterWaitingState();
        // Assuming PrinterSystem has an IsWaitingForPrintJob property
        // if (printerSystem != null) { Debug.Log($"Current Printer Waiting Status: {printerSystem.IsWaitingForPrintJob}"); }
    }

    [ContextMenu("Manually Complete First Task")]
    public void ManualCompleteFirstTask()
    {
        if (activeTaskCompleters.Count > 0 && activeTaskCompleters[0] != null)
        {
            TaskCompleter completer = activeTaskCompleters[0].GetComponent<TaskCompleter>();
            if (completer != null)
            {
                // FIX: Use TaskCompleter's public property TaskIndex
                int compIndex = completer.TaskIndex;
                OnTaskCompleted(compIndex, activeTaskCompleters[0]);
            }
            else
            {
                Debug.LogWarning("[PrintTaskHandler] Could not get TaskCompleter component...");
            }
        }
    }
}