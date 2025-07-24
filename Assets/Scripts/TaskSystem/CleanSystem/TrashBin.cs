using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 垃圾桶组件 - 检测和处理垃圾
/// </summary>
public class TrashBin : MonoBehaviour
{
    [Header("检测设置")]
    [SerializeField] private float detectionRadius = 0.5f; // 检测半径
    [SerializeField] private float detectionHeight = 1f; // 检测高度
    [SerializeField] private float startHeightOffset = 0.1f; // 射线起始高度偏移
    [SerializeField] private int rayCount = 8; // 射线数量
    [SerializeField] private float detectionInterval = 0.1f; // 检测间隔（秒）

    [Header("音效设置")]
    [SerializeField] private AudioSource audioSource; // 音效播放器
    [SerializeField] private AudioClip cleanSound; // 清理音效
    [SerializeField, Range(0f, 1f)] private float cleanVolume = 1f; // 清理音效音量

    [Header("特效设置")]
    [SerializeField] private ParticleSystem cleanEffect; // 清理特效
    [SerializeField] private float effectDuration = 1f; // 特效持续时间

    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true; // 启用调试日志
    [SerializeField] private bool showDebugRays = true; // 显示调试射线
    [SerializeField] private bool showDebugRaysInScene = true; // 在Scene视图中显示射线
    [SerializeField] private Color debugRayColor = Color.yellow; // 调试射线颜色
    [SerializeField] private Color debugHitColor = Color.red; // 命中时的射线颜色
    [SerializeField] private float debugRayDuration = 0.1f; // Debug射线持续时间

    [Header("实时状态显示（只读）")]
    [SerializeField] private int currentDetectedCount = 0; // 当前检测到的垃圾数量
    [SerializeField] private string lastDetectionTime = ""; // 上次检测时间
    [SerializeField] private string nearestRubbishName = ""; // 最近的垃圾名称
    [SerializeField] private float nearestRubbishDistance = 0f; // 最近垃圾距离

    // 私有变量
    private SimplifiedCleanSystem cleanSystem; // 清理系统引用
    private Coroutine detectionCoroutine; // 检测协程
    private HashSet<GameObject> detectedRubbish = new HashSet<GameObject>(); // 已检测到的垃圾
    private Collider trashBinCollider; // 垃圾桶碰撞体

    // 检测位置缓存
    private Vector3 basePosition;
    private Vector3[] rayDirections;

    /// <summary>
    /// 初始化垃圾桶
    /// </summary>
    /// <param name="system">清理系统引用</param>
    public void Initialize(SimplifiedCleanSystem system)
    {
        cleanSystem = system;

        // 验证组件
        ValidateComponents();

        // 预计算射线方向
        CalculateRayDirections();

        // 开始检测
        StartDetection();

        if (enableDebugLog)
            Debug.Log($"[TrashBin] 垃圾桶 {name} 已初始化");
    }

    /// <summary>
    /// 验证组件设置
    /// </summary>
    private void ValidateComponents()
    {
        // 获取或添加AudioSource
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 获取碰撞体
        trashBinCollider = GetComponent<Collider>();
        if (trashBinCollider == null)
        {
            Debug.LogWarning($"[TrashBin] {name} 没有碰撞体组件");
        }

        // 检查特效
        if (cleanEffect == null)
        {
            cleanEffect = GetComponentInChildren<ParticleSystem>();
        }

        // 验证检测参数
        if (detectionRadius <= 0)
        {
            Debug.LogWarning($"[TrashBin] {name} 检测半径应大于0");
            detectionRadius = 0.5f;
        }

        if (detectionHeight <= 0)
        {
            Debug.LogWarning($"[TrashBin] {name} 检测高度应大于0");
            detectionHeight = 1f;
        }

        if (rayCount <= 0)
        {
            Debug.LogWarning($"[TrashBin] {name} 射线数量应大于0");
            rayCount = 8;
        }
    }

    /// <summary>
    /// 预计算射线方向（从垃圾桶内部向上射）
    /// </summary>
    private void CalculateRayDirections()
    {
        rayDirections = new Vector3[rayCount];

        for (int i = 0; i < rayCount; i++)
        {
            float angle = (360f / rayCount) * i;
            float radians = angle * Mathf.Deg2Rad;

            // 计算射线方向：从内部稍微向外向上射
            // 这样可以检测到投入垃圾桶的垃圾
            Vector3 direction = new Vector3(
                Mathf.Cos(radians) * 0.3f, // 轻微向外
                1f, // 主要向上
                Mathf.Sin(radians) * 0.3f  // 轻微向外
            ).normalized;

            rayDirections[i] = direction;
        }

        if (enableDebugLog)
            Debug.Log($"[TrashBin] 已计算 {rayCount} 条射线方向 (从内部向上射)");
    }

    /// <summary>
    /// 开始检测
    /// </summary>
    private void StartDetection()
    {
        if (detectionCoroutine != null)
            StopCoroutine(detectionCoroutine);

        detectionCoroutine = StartCoroutine(DetectionRoutine());
    }

    /// <summary>
    /// 停止检测
    /// </summary>
    private void StopDetection()
    {
        if (detectionCoroutine != null)
        {
            StopCoroutine(detectionCoroutine);
            detectionCoroutine = null;
        }
    }

    /// <summary>
    /// 检测协程
    /// </summary>
    private IEnumerator DetectionRoutine()
    {
        while (true)
        {
            PerformRaycastDetection();
            yield return new WaitForSeconds(detectionInterval);
        }
    }

    /// <summary>
    /// 执行射线检测
    /// </summary>
    private void PerformRaycastDetection()
    {
        // 射线起始位置：垃圾桶位置向上偏移一点点
        basePosition = transform.position + Vector3.up * startHeightOffset;

        int hitCount = 0; // 用于调试统计
        float nearestDistance = float.MaxValue;
        string nearestName = "";

        // 更新检测时间
        lastDetectionTime = System.DateTime.Now.ToString("HH:mm:ss");

        for (int i = 0; i < rayDirections.Length; i++)
        {
            Vector3 worldDirection = transform.TransformDirection(rayDirections[i]);

            // 发射射线（从稍高位置向上射）
            bool hasHit = Physics.Raycast(basePosition, worldDirection, out RaycastHit hit, detectionHeight);

            // 绘制调试射线（在Game视图中显示）
            if (showDebugRays)
            {
                Color rayColor = hasHit ? debugHitColor : debugRayColor;
                Debug.DrawRay(basePosition, worldDirection * detectionHeight, rayColor, debugRayDuration);
            }

            if (hasHit)
            {
                hitCount++;

                // 更新最近垃圾信息
                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearestName = hit.collider.name;
                }

                // 检查击中的物体是否是垃圾
                if (hit.collider.CompareTag("Rubbish"))
                {
                    if (enableDebugLog)
                        Debug.Log($"[TrashBin] 射线 {i} 检测到垃圾: {hit.collider.name} 距离: {hit.distance:F2}m");

                    ProcessRubbish(hit.collider.gameObject);
                }
                else if (enableDebugLog)
                {
                    Debug.Log($"[TrashBin] 射线 {i} 击中非垃圾物体: {hit.collider.name} (标签: {hit.collider.tag})");
                }
            }
        }

        // 更新实时状态
        currentDetectedCount = hitCount;
        nearestRubbishName = nearestName;
        nearestRubbishDistance = nearestDistance == float.MaxValue ? 0f : nearestDistance;

        // 调试信息输出
        if (enableDebugLog && hitCount > 0)
        {
            Debug.Log($"[TrashBin] 检测统计 - 起始位置: {basePosition}, 总射线: {rayDirections.Length}, 命中: {hitCount}, 最近物体: {nearestName} ({nearestDistance:F2}m)");
        }
    }

    /// <summary>
    /// 处理检测到的垃圾
    /// </summary>
    /// <param name="rubbishObject">垃圾对象</param>
    private void ProcessRubbish(GameObject rubbishObject)
    {
        if (rubbishObject == null || detectedRubbish.Contains(rubbishObject))
            return;

        // 添加到已检测列表（防止重复处理）
        detectedRubbish.Add(rubbishObject);

        if (enableDebugLog)
            Debug.Log($"[TrashBin] {name} 检测到垃圾: {rubbishObject.name}");

        // 开始清理垃圾的协程
        StartCoroutine(CleanRubbishRoutine(rubbishObject));
    }

    /// <summary>
    /// 清理垃圾协程
    /// </summary>
    /// <param name="rubbishObject">要清理的垃圾</param>
    private IEnumerator CleanRubbishRoutine(GameObject rubbishObject)
    {
        // 播放清理音效
        PlayCleanSound();

        // 播放清理特效
        PlayCleanEffect();

        // 移除VR交互组件
        RemoveVRInteractable(rubbishObject);

        // 改变标签
        rubbishObject.tag = "Untagged";

        // 等待特效播放完成
        yield return new WaitForSeconds(effectDuration);

        // 通知清理系统
        if (cleanSystem != null)
        {
            cleanSystem.OnRubbishCleanedCallback(rubbishObject);
        }

        // 销毁垃圾对象
        if (rubbishObject != null)
        {
            Destroy(rubbishObject);
        }

        // 从检测列表中移除
        detectedRubbish.Remove(rubbishObject);

        if (enableDebugLog)
            Debug.Log($"[TrashBin] {name} 已清理垃圾: {rubbishObject?.name}");
    }

    /// <summary>
    /// 移除VR交互组件
    /// </summary>
    /// <param name="rubbishObject">垃圾对象</param>
    private void RemoveVRInteractable(GameObject rubbishObject)
    {
        // 移除XR Grab Interactable组件
        XRGrabInteractable grabInteractable = rubbishObject.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            // 如果正在被抓取，先释放
            if (grabInteractable.isSelected)
            {
                grabInteractable.interactionManager.SelectExit(
                    grabInteractable.firstInteractorSelecting,
                    grabInteractable
                );
            }

            Destroy(grabInteractable);

            if (enableDebugLog)
                Debug.Log($"[TrashBin] 已移除 {rubbishObject.name} 的 XRGrabInteractable 组件");
        }

        // 也可以移除其他相关的交互组件
        XRSimpleInteractable simpleInteractable = rubbishObject.GetComponent<XRSimpleInteractable>();
        if (simpleInteractable != null)
        {
            Destroy(simpleInteractable);
        }
    }

    /// <summary>
    /// 播放清理音效
    /// </summary>
    private void PlayCleanSound()
    {
        if (audioSource != null && cleanSound != null)
        {
            audioSource.clip = cleanSound;
            audioSource.volume = cleanVolume;
            audioSource.Play();
        }
    }

    /// <summary>
    /// 播放清理特效
    /// </summary>
    private void PlayCleanEffect()
    {
        if (cleanEffect != null)
        {
            cleanEffect.Play();
        }
    }

    /// <summary>
    /// 手动触发检测（调试用）
    /// </summary>
    [ContextMenu("手动检测")]
    public void ManualDetection()
    {
        if (enableDebugLog)
            Debug.Log($"[TrashBin] 手动触发检测 - 位置: {transform.position}");

        PerformRaycastDetection();
    }

    /// <summary>
    /// 测试单条射线（调试用）
    /// </summary>
    [ContextMenu("测试单条射线")]
    public void TestSingleRay()
    {
        if (rayDirections == null || rayDirections.Length == 0)
        {
            CalculateRayDirections();
        }

        Vector3 basePos = transform.position;
        Vector3 worldDir = transform.TransformDirection(rayDirections[0]);

        bool hasHit = Physics.Raycast(basePos, worldDir, out RaycastHit hit, detectionHeight);

        Debug.Log($"[TrashBin] 测试射线结果:");
        Debug.Log($"  起点: {basePos}");
        Debug.Log($"  方向: {worldDir}");
        Debug.Log($"  距离: {detectionHeight}");
        Debug.Log($"  命中: {hasHit}");

        if (hasHit)
        {
            Debug.Log($"  命中物体: {hit.collider.name}");
            Debug.Log($"  命中位置: {hit.point}");
            Debug.Log($"  命中距离: {hit.distance:F2}m");
            Debug.Log($"  物体标签: {hit.collider.tag}");
        }

        // 绘制测试射线
        Debug.DrawRay(basePos, worldDir * detectionHeight, Color.magenta, 5f);
    }

    /// <summary>
    /// 检查垃圾桶状态（调试用）
    /// </summary>
    [ContextMenu("检查状态")]
    public void CheckStatus()
    {
        Debug.Log($"[TrashBin] === 垃圾桶 {name} 状态 ===");
        Debug.Log($"检测半径: {detectionRadius}m");
        Debug.Log($"检测高度: {detectionHeight}m");
        Debug.Log($"射线数量: {rayCount}");
        Debug.Log($"检测间隔: {detectionInterval}秒");
        Debug.Log($"清理系统引用: {(cleanSystem != null ? "已设置" : "未设置")}");
        Debug.Log($"检测协程状态: {(detectionCoroutine != null ? "运行中" : "未运行")}");
        Debug.Log($"当前检测到的垃圾数量: {detectedRubbish.Count}");
        Debug.Log($"音效组件: {(audioSource != null ? "已设置" : "未设置")}");
        Debug.Log($"特效组件: {(cleanEffect != null ? "已设置" : "未设置")}");
        Debug.Log($"射线方向已计算: {(rayDirections != null && rayDirections.Length > 0)}");

        // 检查附近的垃圾
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRadius * 2f);
        int rubbishCount = 0;
        foreach (var col in nearbyColliders)
        {
            if (col.CompareTag("Rubbish"))
            {
                rubbishCount++;
                Debug.Log($"  附近垃圾: {col.name} 距离: {Vector3.Distance(transform.position, col.transform.position):F2}m");
            }
        }
        Debug.Log($"附近垃圾总数: {rubbishCount}");
    }

    /// <summary>
    /// 测试清理特效（调试用）
    /// </summary>
    [ContextMenu("测试特效")]
    public void TestEffect()
    {
        Debug.Log("[TrashBin] 测试清理特效和音效");
        PlayCleanSound();
        PlayCleanEffect();
    }

    /// <summary>
    /// 重新计算射线方向（调试用）
    /// </summary>
    [ContextMenu("重新计算射线")]
    public void RecalculateRays()
    {
        CalculateRayDirections();
        Debug.Log($"[TrashBin] 已重新计算 {rayCount} 条射线方向");
    }

    /// <summary>
    /// 在Scene视图中显示检测区域（无论是否运行都显示）
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!showDebugRaysInScene) return;

        Vector3 basePos = transform.position;
        Vector3 rayStartPos = basePos + Vector3.up * startHeightOffset;

        // 显示垃圾桶本体（圆柱形轮廓）
        Gizmos.color = Color.cyan;
        DrawWireCircle(basePos, detectionRadius, 16);
        DrawWireCircle(basePos + Vector3.up * detectionHeight, detectionRadius, 16);

        // 绘制垂直线连接上下圆
        for (int i = 0; i < 8; i++)
        {
            float angle = (360f / 8) * i;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 point = basePos + new Vector3(
                Mathf.Cos(radians) * detectionRadius,
                0,
                Mathf.Sin(radians) * detectionRadius
            );
            Gizmos.DrawLine(point, point + Vector3.up * detectionHeight);
        }

        // 显示射线起始位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(rayStartPos, 0.05f);
        Gizmos.DrawLine(basePos, rayStartPos);

        // 显示射线方向（无论是否运行都显示）
        if (rayDirections != null && rayDirections.Length > 0)
        {
            // 运行时显示实际射线
            Gizmos.color = debugRayColor;
            for (int i = 0; i < rayDirections.Length; i++)
            {
                Vector3 worldDir = transform.TransformDirection(rayDirections[i]);
                Gizmos.DrawRay(rayStartPos, worldDir * detectionHeight);

                // 在射线末端画个小球
                Vector3 endPoint = rayStartPos + worldDir * detectionHeight;
                Gizmos.DrawWireSphere(endPoint, 0.03f);
            }
        }
        else
        {
            // 编辑时显示预览射线
            Gizmos.color = Color.yellow;
            for (int i = 0; i < rayCount; i++)
            {
                float angle = (360f / rayCount) * i;
                float radians = angle * Mathf.Deg2Rad;

                Vector3 direction = new Vector3(
                    Mathf.Cos(radians) * 0.3f,
                    1f,
                    Mathf.Sin(radians) * 0.3f
                ).normalized;

                Vector3 worldDir = transform.TransformDirection(direction);
                Gizmos.DrawRay(rayStartPos, worldDir * detectionHeight);

                // 射线末端小球
                Vector3 endPoint = rayStartPos + worldDir * detectionHeight;
                Gizmos.DrawWireSphere(endPoint, 0.03f);
            }
        }

        // 显示中心轴
        Gizmos.color = Color.green;
        Gizmos.DrawLine(basePos, basePos + Vector3.up * detectionHeight);

        // 显示检测信息标签
        Gizmos.color = Color.white;
        Vector3 labelPos = basePos + Vector3.up * (detectionHeight + 0.2f);
        Gizmos.DrawWireCube(labelPos, Vector3.one * 0.1f);

        // 在Scene视图中显示文字信息（如果可能）
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(labelPos + Vector3.up * 0.2f,
            $"检测范围\n半径: {detectionRadius:F1}m\n高度: {detectionHeight:F1}m\n射线: {rayCount}条\n起始偏移: {startHeightOffset:F2}m");
#endif
    }

    /// <summary>
    /// 绘制圆形辅助方法
    /// </summary>
    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    void OnDestroy()
    {
        StopDetection();
    }

    // 属性访问器
    public float DetectionRadius => detectionRadius;
    public float DetectionHeight => detectionHeight;
    public int RayCount => rayCount;
    public bool IsDetecting => detectionCoroutine != null;
}