using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 垃圾桶组件 - 检测和处理垃圾 (使用碰撞体触发检测)
/// </summary>
[RequireComponent(typeof(Collider))] // 确保有碰撞体
public class TrashBin : MonoBehaviour
{
    [Header("音效设置")]
    [SerializeField] private AudioSource audioSource; // 音效播放器
    [SerializeField] private AudioClip cleanSound; // 清理音效
    [SerializeField, Range(0f, 1f)] private float cleanVolume = 1f; // 清理音效音量

    [Header("特效设置")]
    [SerializeField] private ParticleSystem cleanEffect; // 清理特效
    [SerializeField] private float effectDuration = 1f; // 特效持续时间

    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true; // 启用调试日志

    // 私有变量
    private SimplifiedCleanSystem cleanSystem; // 清理系统引用
    private Collider trashBinCollider; // 垃圾桶碰撞体

    // 跟踪正在处理的垃圾，防止重复触发
    private HashSet<GameObject> beingCleanedRubbish = new HashSet<GameObject>();

    /// <summary>
    /// 初始化垃圾桶
    /// </summary>
    /// <param name="system">清理系统引用</param>
    public void Initialize(SimplifiedCleanSystem system)
    {
        cleanSystem = system;

        // 验证组件并配置触发器
        ValidateComponents();

        if (enableDebugLog)
            Debug.Log($"[TrashBin] 垃圾桶 {name} 已初始化 (碰撞体触发检测)");
    }

    /// <summary>
    /// 验证组件设置并确保碰撞体为触发器
    /// </summary>
    private void ValidateComponents()
    {
        // 1. 获取或添加AudioSource
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 2. 获取并配置碰撞体
        trashBinCollider = GetComponent<Collider>();
        if (trashBinCollider == null)
        {
            Debug.LogError($"[TrashBin] {name} 必须要有 Collider 组件才能进行触发检测!");
        }
        else
        {
            // 确保碰撞体是触发器 (Is Trigger)
            if (!trashBinCollider.isTrigger)
            {
                trashBinCollider.isTrigger = true;
                if (enableDebugLog)
                    Debug.LogWarning($"[TrashBin] {name} 的 Collider 已设置为 isTrigger = true");
            }
        }

        // 3. 检查特效
        if (cleanEffect == null)
        {
            cleanEffect = GetComponentInChildren<ParticleSystem>();
        }
    }

    /// <summary>
    /// 碰撞体进入事件 (核心检测逻辑)
    /// </summary>
    /// <param name="other">进入的碰撞体</param>
    void OnTriggerEnter(Collider other)
    {
        // 1. 检查进入的物体是否是垃圾 (通过标签)
        if (other.CompareTag("Rubbish"))
        {
            GameObject rubbishObject = other.gameObject;

            // 2. 检查是否已经在清理中，防止重复触发
            if (rubbishObject != null && !beingCleanedRubbish.Contains(rubbishObject))
            {
                ProcessRubbish(rubbishObject);
            }
        }
    }

    /// <summary>
    /// 处理检测到的垃圾
    /// </summary>
    /// <param name="rubbishObject">垃圾对象</param>
    private void ProcessRubbish(GameObject rubbishObject)
    {
        // 标记为正在处理
        beingCleanedRubbish.Add(rubbishObject);

        if (enableDebugLog)
            Debug.Log($"[TrashBin] {name} 检测到垃圾: {rubbishObject.name} (通过碰撞体触发)");

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

        // 改变标签，防止二次触发
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

        // 从处理列表中移除
        beingCleanedRubbish.Remove(rubbishObject);

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
    /// 注意：碰撞体检测无法手动触发，此方法现在仅用于提示。
    /// </summary>
    [ContextMenu("手动触发 (仅提示)")]
    public void ManualDetection()
    {
        Debug.LogWarning("[TrashBin] 碰撞体检测依赖物理系统，无法手动触发，请将带有'Rubbish'标签的物体放入垃圾桶碰撞体中测试。");
    }

    /// <summary>
    /// 检查垃圾桶状态（调试用）
    /// </summary>
    [ContextMenu("检查状态")]
    public void CheckStatus()
    {
        Debug.Log($"[TrashBin] === 垃圾桶 {name} 状态 ===");
        Debug.Log($"检测类型: 碰撞体触发器 (OnTriggerEnter)");
        Debug.Log($"碰撞体 IsTrigger: {(trashBinCollider != null ? trashBinCollider.isTrigger.ToString() : "N/A")}");
        Debug.Log($"清理系统引用: {(cleanSystem != null ? "已设置" : "未设置")}");
        Debug.Log($"当前正在清理的垃圾数量: {beingCleanedRubbish.Count}");
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

    void OnDestroy()
    {
        // 碰撞体检测无需停止协程
    }
}