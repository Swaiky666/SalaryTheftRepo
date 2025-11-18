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
    [SerializeField] private Color debugRayColor = Color.yellow; // 调试射线颜色

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
    /// 预计算射线方向（圆锥形）
    /// </summary>
    private void CalculateRayDirections()
    {
        rayDirections = new Vector3[rayCount];

        for (int i = 0; i < rayCount; i++)
        {
            float angle = (360f / rayCount) * i;
            float radians = angle * Mathf.Deg2Rad;

            // 在底面圆周上计算点
            Vector3 direction = new Vector3(
                Mathf.Cos(radians) * detectionRadius,
                detectionHeight, // 向上的方向
                Mathf.Sin(radians) * detectionRadius
            ).normalized;

            rayDirections[i] = direction;
        }
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
        basePosition = transform.position;

        for (int i = 0; i < rayDirections.Length; i++)
        {
            Vector3 worldDirection = transform.TransformDirection(rayDirections[i]);

            // 发射射线
            if (Physics.Raycast(basePosition, worldDirection, out RaycastHit hit, detectionHeight))
            {
                // 检查击中的物体是否是垃圾
                if (hit.collider.CompareTag("Rubbish"))
                {
                    ProcessRubbish(hit.collider.gameObject);
                }
            }

            // 绘制调试射线
            if (showDebugRays)
            {
                Debug.DrawRay(basePosition, worldDirection * detectionHeight, debugRayColor, detectionInterval);
            }
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
        PerformRaycastDetection();
    }

    /// <summary>
    /// 检查垃圾桶状态（调试用）
    /// </summary>
    [ContextMenu("检查状态")]
    public void CheckStatus()
    {
        Debug.Log($"[TrashBin] === 垃圾桶 {name} 状态 ===");
        Debug.Log($"检测半径: {detectionRadius}");
        Debug.Log($"检测高度: {detectionHeight}");
        Debug.Log($"射线数量: {rayCount}");
        Debug.Log($"检测间隔: {detectionInterval}秒");
        Debug.Log($"清理系统引用: {(cleanSystem != null ? "已设置" : "未设置")}");
        Debug.Log($"检测协程状态: {(detectionCoroutine != null ? "运行中" : "未运行")}");
        Debug.Log($"当前检测到的垃圾数量: {detectedRubbish.Count}");
        Debug.Log($"音效组件: {(audioSource != null ? "已设置" : "未设置")}");
        Debug.Log($"特效组件: {(cleanEffect != null ? "已设置" : "未设置")}");
    }

    /// <summary>
    /// 测试清理特效（调试用）
    /// </summary>
    [ContextMenu("测试特效")]
    public void TestEffect()
    {
        PlayCleanSound();
        PlayCleanEffect();
    }

    /// <summary>
    /// 在Scene视图中显示检测区域
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!showDebugRays) return;

        // 显示检测范围
        Gizmos.color = Color.cyan;
        Vector3 basePos = transform.position;

        // 绘制检测圆柱体的轮廓
        Gizmos.DrawWireSphere(basePos, detectionRadius);
        Gizmos.DrawWireSphere(basePos + Vector3.up * detectionHeight, detectionRadius);

        // 绘制垂直线
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

        // 显示射线方向
        if (Application.isPlaying && rayDirections != null)
        {
            Gizmos.color = debugRayColor;
            foreach (var direction in rayDirections)
            {
                Vector3 worldDir = transform.TransformDirection(direction);
                Gizmos.DrawRay(basePos, worldDir * detectionHeight);
            }
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