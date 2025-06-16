using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 简化版清理系统 - 管理垃圾桶和垃圾生成
/// </summary>
public class SimplifiedCleanSystem : MonoBehaviour
{
    [Header("垃圾桶设置")]
    [SerializeField] private List<TrashBin> trashBins = new List<TrashBin>(); // 所有垃圾桶

    [Header("垃圾生成设置")]
    [SerializeField] private List<GameObject> rubbishPrefabs = new List<GameObject>(); // 垃圾预制件列表
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>(); // 垃圾生成点
    [SerializeField] private int maxRubbishCount = 10; // 最大垃圾数量
    [SerializeField] private float spawnInterval = 30f; // 生成间隔时间（秒）
    [SerializeField] private int initialRubbishCount = 5; // 初始垃圾数量

    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true; // 启用调试日志

    // 私有变量
    private List<GameObject> activeRubbish = new List<GameObject>(); // 当前场景中的垃圾
    private HashSet<Transform> occupiedSpawnPoints = new HashSet<Transform>(); // 已占用的生成点
    private Coroutine spawnCoroutine; // 生成协程
    private int totalRubbishCleaned = 0; // 总清理垃圾数量

    // 事件
    public System.Action<int> OnRubbishCleaned; // 垃圾被清理事件（清理数量）

    void Start()
    {
        InitializeSystem();
    }

    /// <summary>
    /// 初始化清理系统
    /// </summary>
    private void InitializeSystem()
    {
        // 验证组件设置
        ValidateComponents();

        // 初始化垃圾桶
        InitializeTrashBins();

        // 生成初始垃圾
        SpawnInitialRubbish();

        // 开始垃圾生成协程
        StartRubbishSpawning();

        if (enableDebugLog)
            Debug.Log("[SimplifiedCleanSystem] 清理系统已初始化");
    }

    /// <summary>
    /// 验证组件设置
    /// </summary>
    private void ValidateComponents()
    {
        if (trashBins.Count == 0)
            Debug.LogWarning("[SimplifiedCleanSystem] 未设置垃圾桶列表");

        if (rubbishPrefabs.Count == 0)
            Debug.LogWarning("[SimplifiedCleanSystem] 未设置垃圾预制件列表");

        if (spawnPoints.Count == 0)
            Debug.LogWarning("[SimplifiedCleanSystem] 未设置垃圾生成点列表");

        if (maxRubbishCount <= 0)
        {
            Debug.LogWarning("[SimplifiedCleanSystem] 最大垃圾数量应大于0");
            maxRubbishCount = 10;
        }

        if (spawnInterval <= 0)
        {
            Debug.LogWarning("[SimplifiedCleanSystem] 生成间隔应大于0");
            spawnInterval = 30f;
        }
    }

    /// <summary>
    /// 初始化垃圾桶
    /// </summary>
    private void InitializeTrashBins()
    {
        foreach (var trashBin in trashBins)
        {
            if (trashBin != null)
            {
                trashBin.Initialize(this);
            }
        }

        if (enableDebugLog)
            Debug.Log($"[SimplifiedCleanSystem] 已初始化 {trashBins.Count} 个垃圾桶");
    }

    /// <summary>
    /// 生成初始垃圾
    /// </summary>
    private void SpawnInitialRubbish()
    {
        int spawnCount = Mathf.Min(initialRubbishCount, spawnPoints.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnRubbishAtRandomPoint();
        }

        if (enableDebugLog)
            Debug.Log($"[SimplifiedCleanSystem] 已生成 {spawnCount} 个初始垃圾");
    }

    /// <summary>
    /// 开始垃圾生成协程
    /// </summary>
    private void StartRubbishSpawning()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(RubbishSpawningRoutine());

        if (enableDebugLog)
            Debug.Log("[SimplifiedCleanSystem] 垃圾生成协程已启动");
    }

    /// <summary>
    /// 停止垃圾生成协程
    /// </summary>
    private void StopRubbishSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (enableDebugLog)
            Debug.Log("[SimplifiedCleanSystem] 垃圾生成协程已停止");
    }

    /// <summary>
    /// 垃圾生成协程
    /// </summary>
    private IEnumerator RubbishSpawningRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 检查是否需要生成新垃圾
            if (activeRubbish.Count < maxRubbishCount && occupiedSpawnPoints.Count < spawnPoints.Count)
            {
                SpawnRubbishAtRandomPoint();
            }
        }
    }

    /// <summary>
    /// 在随机位置生成垃圾
    /// </summary>
    private void SpawnRubbishAtRandomPoint()
    {
        if (rubbishPrefabs.Count == 0 || spawnPoints.Count == 0)
            return;

        // 找到可用的生成点
        List<Transform> availableSpawnPoints = new List<Transform>();
        foreach (var point in spawnPoints)
        {
            if (!occupiedSpawnPoints.Contains(point))
            {
                availableSpawnPoints.Add(point);
            }
        }

        if (availableSpawnPoints.Count == 0)
        {
            if (enableDebugLog)
                Debug.Log("[SimplifiedCleanSystem] 没有可用的生成点");
            return;
        }

        // 随机选择生成点和垃圾预制件
        Transform selectedPoint = availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];
        GameObject selectedPrefab = rubbishPrefabs[Random.Range(0, rubbishPrefabs.Count)];

        // 生成垃圾
        GameObject newRubbish = Instantiate(selectedPrefab, selectedPoint.position, selectedPoint.rotation);

        // 确保垃圾有正确的标签
        if (!newRubbish.CompareTag("Rubbish"))
        {
            newRubbish.tag = "Rubbish";
        }

        // 添加到活跃垃圾列表
        activeRubbish.Add(newRubbish);
        occupiedSpawnPoints.Add(selectedPoint);

        // 添加RubbishItem组件（如果没有的话）
        RubbishItem rubbishItem = newRubbish.GetComponent<RubbishItem>();
        if (rubbishItem == null)
        {
            rubbishItem = newRubbish.AddComponent<RubbishItem>();
        }
        rubbishItem.Initialize(selectedPoint, this);

        if (enableDebugLog)
            Debug.Log($"[SimplifiedCleanSystem] 在 {selectedPoint.name} 生成垃圾: {newRubbish.name}");
    }

    /// <summary>
    /// 垃圾被清理回调（由TrashBin调用）
    /// </summary>
    /// <param name="rubbishObject">被清理的垃圾对象</param>
    public void OnRubbishCleanedCallback(GameObject rubbishObject)
    {
        if (rubbishObject == null) return;

        // 获取垃圾物品组件
        RubbishItem rubbishItem = rubbishObject.GetComponent<RubbishItem>();
        if (rubbishItem != null)
        {
            // 释放生成点
            occupiedSpawnPoints.Remove(rubbishItem.SpawnPoint);
        }

        // 从活跃垃圾列表中移除
        if (activeRubbish.Contains(rubbishObject))
        {
            activeRubbish.Remove(rubbishObject);
        }

        // 增加清理计数
        totalRubbishCleaned++;

        // 触发清理事件
        OnRubbishCleaned?.Invoke(1);

        if (enableDebugLog)
            Debug.Log($"[SimplifiedCleanSystem] 垃圾已清理: {rubbishObject.name}，总清理数量: {totalRubbishCleaned}");
    }

    /// <summary>
    /// 获取当前垃圾数量
    /// </summary>
    public int GetCurrentRubbishCount()
    {
        // 清理已被销毁的垃圾引用
        activeRubbish.RemoveAll(rubbish => rubbish == null);
        return activeRubbish.Count;
    }

    /// <summary>
    /// 获取总清理垃圾数量
    /// </summary>
    public int GetTotalCleanedCount() => totalRubbishCleaned;

    /// <summary>
    /// 清理所有垃圾（调试用）
    /// </summary>
    [ContextMenu("清理所有垃圾")]
    public void CleanAllRubbish()
    {
        List<GameObject> rubbishToClean = new List<GameObject>(activeRubbish);
        foreach (var rubbish in rubbishToClean)
        {
            if (rubbish != null)
            {
                OnRubbishCleanedCallback(rubbish);
                Destroy(rubbish);
            }
        }

        if (enableDebugLog)
            Debug.Log("[SimplifiedCleanSystem] 已清理所有垃圾");
    }

    /// <summary>
    /// 强制生成垃圾（调试用）
    /// </summary>
    [ContextMenu("强制生成垃圾")]
    public void ForceSpawnRubbish()
    {
        SpawnRubbishAtRandomPoint();
    }

    /// <summary>
    /// 检查清理系统状态（调试用）
    /// </summary>
    [ContextMenu("检查系统状态")]
    public void CheckSystemStatus()
    {
        Debug.Log($"[SimplifiedCleanSystem] === 清理系统状态 ===");
        Debug.Log($"垃圾桶数量: {trashBins.Count}");
        Debug.Log($"垃圾预制件数量: {rubbishPrefabs.Count}");
        Debug.Log($"生成点数量: {spawnPoints.Count}");
        Debug.Log($"当前垃圾数量: {GetCurrentRubbishCount()}");
        Debug.Log($"最大垃圾数量: {maxRubbishCount}");
        Debug.Log($"已占用生成点: {occupiedSpawnPoints.Count}");
        Debug.Log($"总清理数量: {totalRubbishCleaned}");
        Debug.Log($"生成间隔: {spawnInterval}秒");
        Debug.Log($"生成协程状态: {(spawnCoroutine != null ? "运行中" : "未运行")}");
    }

    void OnDestroy()
    {
        // 停止生成协程
        StopRubbishSpawning();
    }

    // 属性访问器
    public int MaxRubbishCount => maxRubbishCount;
    public float SpawnInterval => spawnInterval;
    public int CurrentRubbishCount => GetCurrentRubbishCount();
    public int TotalCleanedCount => totalRubbishCleaned;
}