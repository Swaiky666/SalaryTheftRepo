using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 简单清理系统设置工具
/// 帮助快速设置基础的清理系统
/// </summary>
public class SimpleCleanSetup : MonoBehaviour
{
    [Header("系统组件")]
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private GameLogicSystem gameLogicSystem;

    [Header("清理系统设置")]
    [SerializeField] private int maxRubbishCount = 10;
    [SerializeField] private float spawnInterval = 30f;
    [SerializeField] private int initialRubbishCount = 5;

    [Header("垃圾桶设置")]
    [SerializeField] private List<Transform> trashBinPositions = new List<Transform>();
    [SerializeField] private GameObject trashBinPrefab;

    [Header("垃圾生成设置")]
    [SerializeField] private List<Transform> spawnPositions = new List<Transform>();
    [SerializeField] private List<GameObject> rubbishPrefabs = new List<GameObject>();

    [Header("任务设置")]
    [SerializeField] private int rubbishToCleanForCompletion = 5;
    [SerializeField] private float workProgressPerRubbish = 2f;

    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true;

    /// <summary>
    /// 一键设置清理系统
    /// </summary>
    [ContextMenu("设置清理系统")]
    public void SetupCleanSystem()
    {
        if (enableDebugLog)
            Debug.Log("[SimpleCleanSetup] 开始设置清理系统...");

        try
        {
            // 1. 查找系统组件
            FindSystemComponents();

            // 2. 创建清理系统
            CreateCleanSystem();

            // 3. 创建垃圾桶
            CreateTrashBins();

            // 4. 创建清理任务处理器
            CreateCleanTaskHandler();

            // 5. 验证设置
            ValidateSetup();

            if (enableDebugLog)
                Debug.Log("[SimpleCleanSetup] ✅ 清理系统设置完成！");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SimpleCleanSetup] 设置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 查找系统组件
    /// </summary>
    private void FindSystemComponents()
    {
        if (taskManager == null)
            taskManager = FindObjectOfType<TaskManager>();

        if (gameLogicSystem == null)
            gameLogicSystem = FindObjectOfType<GameLogicSystem>();

        if (enableDebugLog)
        {
            Debug.Log($"[SimpleCleanSetup] TaskManager: {(taskManager != null ? "找到" : "未找到")}");
            Debug.Log($"[SimpleCleanSetup] GameLogicSystem: {(gameLogicSystem != null ? "找到" : "未找到")}");
        }
    }

    /// <summary>
    /// 创建清理系统
    /// </summary>
    private void CreateCleanSystem()
    {
        SimplifiedCleanSystem cleanSystem = FindObjectOfType<SimplifiedCleanSystem>();
        if (cleanSystem == null)
        {
            GameObject cleanSystemObject = new GameObject("CleanSystem");
            cleanSystem = cleanSystemObject.AddComponent<SimplifiedCleanSystem>();

            if (enableDebugLog)
                Debug.Log("[SimpleCleanSetup] 创建了CleanSystem");
        }

        // 设置垃圾预制件和生成点
        // 注意：这里需要通过公共字段或方法设置，具体取决于SimplifiedCleanSystem的实现
        if (enableDebugLog)
            Debug.Log("[SimpleCleanSetup] 请手动设置CleanSystem的垃圾预制件和生成点");
    }

    /// <summary>
    /// 创建垃圾桶
    /// </summary>
    private void CreateTrashBins()
    {
        foreach (Transform position in trashBinPositions)
        {
            if (position == null) continue;

            GameObject trashBinObject;

            if (trashBinPrefab != null)
            {
                trashBinObject = Instantiate(trashBinPrefab, position.position, position.rotation);
            }
            else
            {
                trashBinObject = CreateSimpleTrashBin(position.position, position.rotation);
            }

            // 确保有TrashBin组件
            TrashBin trashBin = trashBinObject.GetComponent<TrashBin>();
            if (trashBin == null)
            {
                trashBin = trashBinObject.AddComponent<TrashBin>();
            }

            if (enableDebugLog)
                Debug.Log($"[SimpleCleanSetup] 创建垃圾桶: {trashBinObject.name}");
        }
    }

    /// <summary>
    /// 创建简单垃圾桶
    /// </summary>
    private GameObject CreateSimpleTrashBin(Vector3 position, Quaternion rotation)
    {
        GameObject trashBin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trashBin.name = "TrashBin";
        trashBin.transform.position = position;
        trashBin.transform.rotation = rotation;
        trashBin.transform.localScale = new Vector3(1f, 1.5f, 1f);

        // 设置材质颜色
        Renderer renderer = trashBin.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.3f, 0.3f, 0.3f);
            renderer.material = material;
        }

        return trashBin;
    }

    /// <summary>
    /// 创建清理任务处理器
    /// </summary>
    private void CreateCleanTaskHandler()
    {
        CleanTaskHandler handler = FindObjectOfType<CleanTaskHandler>();
        if (handler == null)
        {
            GameObject handlerObject = new GameObject("CleanTaskHandler");
            handler = handlerObject.AddComponent<CleanTaskHandler>();

            if (enableDebugLog)
                Debug.Log("[SimpleCleanSetup] 创建了CleanTaskHandler");
        }
    }

    /// <summary>
    /// 验证设置
    /// </summary>
    private void ValidateSetup()
    {
        SimplifiedCleanSystem cleanSystem = FindObjectOfType<SimplifiedCleanSystem>();
        CleanTaskHandler taskHandler = FindObjectOfType<CleanTaskHandler>();

        if (enableDebugLog)
        {
            Debug.Log($"[SimpleCleanSetup] === 设置验证 ===");
            Debug.Log($"CleanSystem: {(cleanSystem != null ? "✓" : "✗")}");
            Debug.Log($"CleanTaskHandler: {(taskHandler != null ? "✓" : "✗")}");
            Debug.Log($"TaskManager: {(taskManager != null ? "✓" : "✗")}");
            Debug.Log($"垃圾桶位置数量: {trashBinPositions.Count}");
            Debug.Log($"生成点数量: {spawnPositions.Count}");
            Debug.Log($"垃圾预制件数量: {rubbishPrefabs.Count}");
        }

        // 提醒需要手动设置的内容
        Debug.Log("[SimpleCleanSetup] 请记得手动设置：");
        Debug.Log("1. CleanSystem的垃圾预制件列表");
        Debug.Log("2. CleanSystem的生成点列表");
        Debug.Log("3. CleanSystem的垃圾桶列表");
        Debug.Log("4. TaskManager的CleanTaskHandler引用");
    }

    /// <summary>
    /// 自动创建生成点
    /// </summary>
    [ContextMenu("自动创建生成点")]
    public void CreateSpawnPoints()
    {
        GameObject parent = new GameObject("RubbishSpawnPoints");
        parent.transform.position = transform.position;

        for (int i = 0; i < 10; i++)
        {
            Vector2 randomPos = Random.insideUnitCircle * 8f;
            Vector3 spawnPos = transform.position + new Vector3(randomPos.x, 0.1f, randomPos.y);

            GameObject spawnPoint = new GameObject($"SpawnPoint_{i + 1:D2}");
            spawnPoint.transform.position = spawnPos;
            spawnPoint.transform.parent = parent.transform;

            spawnPositions.Add(spawnPoint.transform);
        }

        if (enableDebugLog)
            Debug.Log("[SimpleCleanSetup] 自动创建了10个生成点");
    }

    /// <summary>
    /// 清理创建的对象
    /// </summary>
    [ContextMenu("清理创建的对象")]
    public void CleanupCreatedObjects()
    {
        // 清理CleanSystem
        SimplifiedCleanSystem cleanSystem = FindObjectOfType<SimplifiedCleanSystem>();
        if (cleanSystem != null)
        {
            DestroyImmediate(cleanSystem.gameObject);
        }

        // 清理CleanTaskHandler
        CleanTaskHandler taskHandler = FindObjectOfType<CleanTaskHandler>();
        if (taskHandler != null)
        {
            DestroyImmediate(taskHandler.gameObject);
        }

        // 清理垃圾桶
        TrashBin[] trashBins = FindObjectsOfType<TrashBin>();
        foreach (var bin in trashBins)
        {
            DestroyImmediate(bin.gameObject);
        }

        // 清理生成点
        GameObject spawnPointsParent = GameObject.Find("RubbishSpawnPoints");
        if (spawnPointsParent != null)
        {
            DestroyImmediate(spawnPointsParent);
        }

        spawnPositions.Clear();

        if (enableDebugLog)
            Debug.Log("[SimpleCleanSetup] 已清理所有创建的对象");
    }

    void OnDrawGizmosSelected()
    {
        // 显示垃圾桶位置
        Gizmos.color = Color.red;
        foreach (Transform pos in trashBinPositions)
        {
            if (pos != null)
            {
                Gizmos.DrawWireSphere(pos.position, 0.5f);
            }
        }

        // 显示生成点
        Gizmos.color = Color.green;
        foreach (Transform spawn in spawnPositions)
        {
            if (spawn != null)
            {
                Gizmos.DrawWireCube(spawn.position, Vector3.one * 0.3f);
            }
        }
    }
}