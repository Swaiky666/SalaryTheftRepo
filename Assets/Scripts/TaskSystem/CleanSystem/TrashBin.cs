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
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource; // 音效播放器
    [SerializeField] private AudioClip cleanSound; // 清理音效
    [SerializeField, Range(0f, 1f)] private float cleanVolume = 1f; // 清理音效音量

    [Header("Effect Settings")]
    [SerializeField] private ParticleSystem cleanEffect; // 清理特效
    [SerializeField] private float effectDuration = 1f; // 特效持续时间

    [Header("Debug Settings")]
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

        // 获取碰撞体并确保是触发器
        trashBinCollider = GetComponent<Collider>();
        if (trashBinCollider != null)
        {
            trashBinCollider.isTrigger = true;
        }
        else if (enableDebugLog)
        {
            Debug.LogError("[TrashBin] Collider not found on TrashBin object!");
        }
    }

    /// <summary>
    /// 触发器进入回调
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Rubbish"))
        {
            RubbishItem rubbishItem = other.GetComponent<RubbishItem>();
            if (rubbishItem != null && !beingCleanedRubbish.Contains(other.gameObject))
            {
                ProcessRubbish(rubbishItem, other.gameObject);
            }
        }
    }

    /// <summary>
    /// 处理进入垃圾桶的垃圾
    /// </summary>
    /// <param name="rubbishItem">垃圾物品组件</param>
    /// <param name="rubbishObject">垃圾对象</param>
    private void ProcessRubbish(RubbishItem rubbishItem, GameObject rubbishObject)
    {
        if (!rubbishItem.CanBeCleaned())
        {
            if (enableDebugLog) Debug.Log($"[TrashBin] Rubbish {rubbishObject.name} cannot be cleaned (already cleaned or not interactable).");
            return;
        }

        // 标记为正在处理，防止重复触发
        beingCleanedRubbish.Add(rubbishObject);

        // 1. 播放音效和特效
        PlayCleanSound();
        PlayCleanEffect();

        // 2. 尝试让 RubbishItem 完成清理流程
        rubbishItem.TryClean();

        // 3. 移除标记（需要延迟，因为 RubbishItem 内部有协程）
        StartCoroutine(RemoveFromProcessingList(rubbishObject));

        if (enableDebugLog)
            Debug.Log($"[TrashBin] Rubbish {rubbishObject.name} detected and starting clean process.");
    }

    /// <summary>
    /// 延迟移除正在处理列表
    /// </summary>
    private IEnumerator RemoveFromProcessingList(GameObject rubbishObject)
    {
        // 等待一段时间，确保 RubbishItem.TryClean() 内部逻辑完成
        yield return new WaitForSeconds(1.0f);

        beingCleanedRubbish.Remove(rubbishObject);
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
    [ContextMenu("Manual Trigger (Hint Only)")]
    public void ManualDetection()
    {
        Debug.LogWarning("[TrashBin] Collider detection relies on the physics system and cannot be manually triggered. Please place an object with the 'Rubbish' tag into the trash bin collider for testing.");
    }

    /// <summary>
    /// 检查垃圾桶状态（调试用）
    /// </summary>
    [ContextMenu("Check Status")]
    public void CheckStatus()
    {
        Debug.Log($"[TrashBin] === Trash Bin {name} Status ===");
        Debug.Log($"Detection Type: Collider Trigger (OnTriggerEnter)");
        Debug.Log($"Collider IsTrigger: {(trashBinCollider != null ? trashBinCollider.isTrigger.ToString() : "N/A")}");
        Debug.Log($"Clean System Reference: {(cleanSystem != null ? "Set" : "Not Set")}");
        Debug.Log($"Currently Processing Rubbish Count: {beingCleanedRubbish.Count}");
    }
}