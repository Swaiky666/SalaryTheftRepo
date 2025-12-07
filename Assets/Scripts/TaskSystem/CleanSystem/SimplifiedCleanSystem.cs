using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 简化版清理系统 - 管理垃圾桶和垃圾生成
/// </summary>
public class SimplifiedCleanSystem : MonoBehaviour
{
    [Header("Trash Bin Settings")]
    [SerializeField] private List<TrashBin> trashBins = new List<TrashBin>(); // 所有垃圾桶列表

    [Header("Rubbish Spawn Settings")]
    [SerializeField] private List<GameObject> rubbishPrefabs = new List<GameObject>(); // 垃圾预制件列表
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>(); // 垃圾生成点列表
    [SerializeField] private int maxRubbishCount = 10; // 场景中最大垃圾数量
    [SerializeField] private float spawnInterval = 30f; // 垃圾生成间隔时间（秒）
    [SerializeField] private int initialRubbishCount = 5; // 初始生成的垃圾数量

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLog = true; // 启用调试日志

    // 私有变量
    private List<GameObject> activeRubbish = new List<GameObject>(); // 当前场景中的活跃垃圾对象
    private HashSet<Transform> occupiedSpawnPoints = new HashSet<Transform>(); // 已占用的生成点
    private Coroutine spawnCoroutine; // 垃圾生成协程
    private int totalRubbishCleaned = 0; // 迄今为止总清理垃圾数量

    // 事件
    public System.Action<int> OnRubbishCleaned; // 垃圾被清理事件（参数为清理的数量）

    void Start()
    {
        InitializeSystem();
    }

    /// <summary>
    /// 初始化清理系统
    /// </summary>
    private void InitializeSystem()
    {
        // 1. 初始化垃圾桶
        foreach (var bin in trashBins)
        {
            if (bin != null)
            {
                bin.Initialize(this);
            }
        }

        // 2. 生成初始垃圾
        SpawnInitialRubbish();

        // 3. 启动定时生成协程
        StartSpawnCoroutine();

        if (enableDebugLog)
            Debug.Log("[SimplifiedCleanSystem] System Initialized.");
    }

    /// <summary>
    /// 生成初始垃圾
    /// </summary>
    private void SpawnInitialRubbish()
    {
        for (int i = 0; i < initialRubbishCount; i++)
        {
            SpawnRubbishAtRandomPoint();
        }
        if (enableDebugLog)
            Debug.Log($"[SimplifiedCleanSystem] Initial {GetCurrentRubbishCount()} rubbish spawned.");
    }

    /// <summary>
    /// 启动垃圾生成协程
    /// </summary>
    private void StartSpawnCoroutine()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(RubbishSpawnRoutine());
    }

    /// <summary>
    /// 垃圾生成协程
    /// </summary>
    private IEnumerator RubbishSpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (GetCurrentRubbishCount() < maxRubbishCount)
            {
                SpawnRubbishAtRandomPoint();
            }
            else if (enableDebugLog)
            {
                // Debug.Log("[SimplifiedCleanSystem] Max rubbish count reached. Skipping spawn.");
            }
        }
    }

    /// <summary>
    /// 在随机可用生成点生成垃圾
    /// </summary>
    private bool SpawnRubbishAtRandomPoint()
    {
        if (rubbishPrefabs.Count == 0 || spawnPoints.Count == 0)
        {
            if (enableDebugLog) Debug.LogWarning("[SimplifiedCleanSystem] Rubbish prefabs or spawn points are not set.");
            return false;
        }

        // 查找未被占用的生成点
        List<Transform> availableSpawnPoints = spawnPoints.Where(p => !occupiedSpawnPoints.Contains(p)).ToList();

        if (availableSpawnPoints.Count == 0)
        {
            if (enableDebugLog) Debug.Log("[SimplifiedCleanSystem] No available spawn points.");
            return false;
        }

        // 随机选择一个点和预制件
        Transform spawnPoint = availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];
        GameObject rubbishPrefab = rubbishPrefabs[Random.Range(0, rubbishPrefabs.Count)];

        // 实例化垃圾
        GameObject newRubbish = Instantiate(rubbishPrefab, spawnPoint.position, spawnPoint.rotation);

        // 设置 RubbishItem 引用
        RubbishItem rubbishItem = newRubbish.GetComponent<RubbishItem>();
        if (rubbishItem != null)
        {
            rubbishItem.SetCleanSystem(this);
            rubbishItem.SetSpawnPoint(spawnPoint);
        }

        // 更新状态
        activeRubbish.Add(newRubbish);
        occupiedSpawnPoints.Add(spawnPoint);

        if (enableDebugLog) Debug.Log($"[SimplifiedCleanSystem] Rubbish spawned at {spawnPoint.name}. Current count: {GetCurrentRubbishCount()}");

        return true;
    }

    /// <summary>
    /// 垃圾被清理时的回调 (由 RubbishItem 调用)
    /// </summary>
    public void OnRubbishCleanedCallback(GameObject cleanedRubbish)
    {
        if (activeRubbish.Contains(cleanedRubbish))
        {
            // 找到并释放占用的生成点
            RubbishItem item = cleanedRubbish.GetComponent<RubbishItem>();
            if (item != null && item.SpawnPoint != null)
            {
                occupiedSpawnPoints.Remove(item.SpawnPoint);
            }

            activeRubbish.Remove(cleanedRubbish);
            totalRubbishCleaned++;

            // 触发事件，通知 TaskHandler
            OnRubbishCleaned?.Invoke(1);

            if (enableDebugLog)
                Debug.Log($"[SimplifiedCleanSystem] Rubbish cleaned. Total cleaned: {totalRubbishCleaned}. Active count: {GetCurrentRubbishCount()}");
        }
    }

    /// <summary>
    /// 获取当前垃圾数量
    /// </summary>
    public int GetCurrentRubbishCount()
    {
        // 移除所有 null 的引用，防止计数错误
        activeRubbish.RemoveAll(item => item == null);
        return activeRubbish.Count;
    }

    /// <summary>
    /// 强制清理所有垃圾（调试用）
    /// </summary>
    [ContextMenu("Force Clean All Rubbish")]
    public void ForceCleanAllRubbish()
    {
        // 创建一个副本列表，防止在循环中修改列表
        List<GameObject> rubbishToClean = new List<GameObject>(activeRubbish);

        foreach (GameObject rubbish in rubbishToClean)
        {
            if (rubbish != null)
            {
                OnRubbishCleanedCallback(rubbish);
                Destroy(rubbish);
            }
        }

        if (enableDebugLog)
            Debug.Log("[SimplifiedCleanSystem] All rubbish forced cleaned.");
    }

    /// <summary>
    /// 强制生成垃圾（调试用）
    /// </summary>
    [ContextMenu("Force Spawn Rubbish")]
    public void ForceSpawnRubbish()
    {
        SpawnRubbishAtRandomPoint();
    }

    /// <summary>
    /// 检查清理系统状态（调试用）
    /// </summary>
    [ContextMenu("Check System Status")]
    public void CheckSystemStatus()
    {
        Debug.Log($"[SimplifiedCleanSystem] === Clean System Status ===");
        Debug.Log($"Trash Bin Count: {trashBins.Count}");
        Debug.Log($"Rubbish Prefab Count: {rubbishPrefabs.Count}");
        Debug.Log($"Spawn Point Count: {spawnPoints.Count}");
        Debug.Log($"Current Rubbish Count: {GetCurrentRubbishCount()}");
        Debug.Log($"Max Rubbish Count: {maxRubbishCount}");
        Debug.Log($"Occupied Spawn Points: {occupiedSpawnPoints.Count}");
        Debug.Log($"Total Rubbish Cleaned: {totalRubbishCleaned}");
        Debug.Log($"Spawn Interval: {spawnInterval}s");
        Debug.Log($"Spawn Coroutine Status: {(spawnCoroutine != null ? "Active" : "Inactive")}");
    }
}