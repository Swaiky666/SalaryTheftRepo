using System.Collections;
using UnityEngine;

public class PrinterSystem : MonoBehaviour
{
    [Header("打印设置")]
    [SerializeField] private Transform spawnPosition; // 任务道具实例化位置
    [SerializeField] private GameObject taskItemPrefab; // 任务道具预制件

    [Header("动画设置")]
    [SerializeField] private Animator printerAnimator; // 打印机Animator
    [SerializeField] private float productionTime = 3f; // 生产时间（摇晃动画持续时间）

    [Header("特效设置")]
    [SerializeField] private ParticleSystem smokeEffect; // 烟雾粒子特效
    [SerializeField] private float smokeDelay = 0f; // 烟雾特效延迟时间

    [Header("音效设置")]
    [SerializeField] private AudioSource printAudioSource; // 打印音效播放器
    [SerializeField] private AudioClip printStartSound; // 打印开始音效
    [SerializeField] private AudioClip printCompleteSound; // 打印完成音效
    [SerializeField, Range(0f, 1f)] private float printVolume = 1f; // 打印音效音量

    [Header("UI设置")]
    [SerializeField] private TMPro.TextMeshProUGUI waitingJobText; // 等待任务提示文本

    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true; // 启用调试日志

    // 私有变量
    private bool isPrinting = false; // 是否正在打印
    private bool isWaitingForPrintJob = false; // 是否等待打印任务
    private Coroutine printCoroutine; // 打印协程

    // 事件
    public System.Action<GameObject> OnTaskItemPrinted; // 任务道具打印完成事件

    // Animator 参数名称常量
    private const string ANIMATOR_IS_PRINTING = "IsPrinting";

    void Start()
    {
        // 初始化检查
        ValidateComponents();

        // 确保烟雾特效初始状态是停止的
        if (smokeEffect != null && smokeEffect.isPlaying)
        {
            smokeEffect.Stop();
        }

        // 确保动画初始状态为待机
        SetAnimatorIdle();

        // 初始化UI状态
        UpdateWaitingJobUI();
    }

    /// <summary>
    /// 设置Animator为待机状态
    /// </summary>
    private void SetAnimatorIdle()
    {
        if (printerAnimator == null) return;

        printerAnimator.SetBool(ANIMATOR_IS_PRINTING, false);

        if (enableDebugLog)
            Debug.Log("[PrinterSystem] Animator已设置为待机状态");
    }

    /// <summary>
    /// 验证组件设置
    /// </summary>
    private void ValidateComponents()
    {
        if (spawnPosition == null)
        {
            Debug.LogWarning("[PrinterSystem] 未设置spawnPosition，将使用打印机自身位置");
            spawnPosition = transform;
        }

        if (taskItemPrefab == null)
            Debug.LogWarning("[PrinterSystem] 未设置taskItemPrefab，无法实例化任务道具");

        if (printerAnimator == null)
            Debug.LogWarning("[PrinterSystem] 未设置printerAnimator，将跳过动画播放");

        if (smokeEffect == null)
            Debug.LogWarning("[PrinterSystem] 未设置smokeEffect，将跳过烟雾特效");

        if (waitingJobText == null)
            Debug.LogWarning("[PrinterSystem] 未设置waitingJobText，将跳过等待提示显示");

        if (productionTime <= 0)
        {
            Debug.LogWarning("[PrinterSystem] 生产时间应大于0，当前值: " + productionTime);
            productionTime = 3f; // 设置默认值
        }
    }

    /// <summary>
    /// 开始打印任务道具
    /// </summary>
    /// <returns>是否成功开始打印</returns>
    public bool StartPrinting()
    {
        if (isPrinting)
        {
            if (enableDebugLog)
                Debug.Log("[PrinterSystem] 打印机正在工作中，无法开始新的打印任务");
            return false;
        }

        if (taskItemPrefab == null)
        {
            Debug.LogError("[PrinterSystem] 无法开始打印：未设置taskItemPrefab");
            return false;
        }

        isPrinting = true;

        if (printCoroutine != null)
            StopCoroutine(printCoroutine);

        printCoroutine = StartCoroutine(PrintingProcess());

        return true;
    }

    /// <summary>
    /// 停止打印过程
    /// </summary>
    public void StopPrinting()
    {
        if (!isPrinting) return;

        isPrinting = false;

        if (printCoroutine != null)
        {
            StopCoroutine(printCoroutine);
            printCoroutine = null;
        }

        // 设置动画回到待机状态
        SetAnimatorIdle();

        // 停止烟雾特效
        if (smokeEffect != null && smokeEffect.isPlaying)
        {
            smokeEffect.Stop();
        }

        if (enableDebugLog)
            Debug.Log("[PrinterSystem] 打印过程已停止，已切换回待机状态");
    }

    /// <summary>
    /// 打印过程协程
    /// </summary>
    private IEnumerator PrintingProcess()
    {
        if (enableDebugLog)
            Debug.Log("[PrinterSystem] 🖨️ 开始打印任务道具...");

        // 自动隐藏等待打印任务提示
        if (isWaitingForPrintJob)
        {
            SetWaitingForPrintJob(false);
        }

        // 第1步：播放打印开始音效
        PlayPrintSound(printStartSound);

        // 第2步：开始摇晃动画
        StartShakeAnimation();

        // 第3步：等待生产时间（重要：这里是等待生产过程）
        if (enableDebugLog)
            Debug.Log($"[PrinterSystem] ⏱️ 生产中，等待 {productionTime} 秒...");

        yield return new WaitForSeconds(productionTime);

        // 第4步：停止摇晃动画，回到待机
        StopShakeAnimation();

        // 第5步：播放烟雾特效
        if (smokeEffect != null)
        {
            if (enableDebugLog)
                Debug.Log("[PrinterSystem] 💨 播放烟雾特效...");

            smokeEffect.Play();
        }

        // 第6步：等待烟雾延迟
        if (smokeDelay > 0)
        {
            yield return new WaitForSeconds(smokeDelay);
        }

        // 第7步：实例化任务道具
        GameObject spawnedItem = SpawnTaskItem();

        // 第8步：播放完成音效
        PlayPrintSound(printCompleteSound);

        // 第9步：触发完成事件
        OnTaskItemPrinted?.Invoke(spawnedItem);

        isPrinting = false;

        if (enableDebugLog)
            Debug.Log("[PrinterSystem] ✅ 打印任务完成！");
    }

    /// <summary>
    /// 开始摇晃动画
    /// </summary>
    private void StartShakeAnimation()
    {
        if (printerAnimator == null)
        {
            if (enableDebugLog)
                Debug.Log("[PrinterSystem] 跳过摇晃动画（未设置Animator）");
            return;
        }

        printerAnimator.SetBool(ANIMATOR_IS_PRINTING, true);

        if (enableDebugLog)
            Debug.Log($"[PrinterSystem] 📳 开始摇晃动画，持续时间: {productionTime}秒");
    }

    /// <summary>
    /// 停止摇晃动画
    /// </summary>
    private void StopShakeAnimation()
    {
        if (printerAnimator == null) return;

        printerAnimator.SetBool(ANIMATOR_IS_PRINTING, false);

        if (enableDebugLog)
            Debug.Log("[PrinterSystem] 🛑 摇晃动画已停止，切换回待机状态");
    }

    /// <summary>
    /// 实例化任务道具
    /// </summary>
    /// <returns>实例化的道具GameObject</returns>
    private GameObject SpawnTaskItem()
    {
        if (taskItemPrefab == null)
        {
            Debug.LogError("[PrinterSystem] 无法实例化任务道具：taskItemPrefab为空");
            return null;
        }

        Vector3 spawnPos = spawnPosition.position;
        Quaternion spawnRot = spawnPosition.rotation;

        GameObject spawnedItem = Instantiate(taskItemPrefab, spawnPos, spawnRot);

        if (enableDebugLog)
            Debug.Log($"[PrinterSystem] 📄 任务道具已生成：{spawnedItem.name} 位置：{spawnPos}");

        return spawnedItem;
    }

    /// <summary>
    /// 播放打印音效
    /// </summary>
    /// <param name="audioClip">要播放的音效</param>
    private void PlayPrintSound(AudioClip audioClip)
    {
        if (printAudioSource == null || audioClip == null) return;

        printAudioSource.clip = audioClip;
        printAudioSource.volume = printVolume;
        printAudioSource.Play();

        if (enableDebugLog)
            Debug.Log($"[PrinterSystem] 🔊 播放音效：{audioClip.name}");
    }

    /// <summary>
    /// 设置等待打印任务状态（外部调用）
    /// </summary>
    /// <param name="isWaiting">是否等待打印任务</param>
    public void SetWaitingForPrintJob(bool isWaiting)
    {
        if (isWaitingForPrintJob == isWaiting) return; // 状态没有改变，不需要更新

        isWaitingForPrintJob = isWaiting;
        UpdateWaitingJobUI();

        if (enableDebugLog)
            Debug.Log($"[PrinterSystem] 等待打印任务状态已设置为: {isWaiting}");
    }

    /// <summary>
    /// 更新等待任务UI显示
    /// </summary>
    private void UpdateWaitingJobUI()
    {
        if (waitingJobText == null) return;

        waitingJobText.gameObject.SetActive(isWaitingForPrintJob);

        if (enableDebugLog)
        {
            string status = isWaitingForPrintJob ? "显示" : "隐藏";
            Debug.Log($"[PrinterSystem] 等待任务提示文本已{status}");
        }
    }

    /// <summary>
    /// 获取等待打印任务状态
    /// </summary>
    /// <returns>是否正在等待打印任务</returns>
    public bool IsWaitingForPrintJob => isWaitingForPrintJob;

    /// <summary>
    /// 手动触发打印（用于测试或外部调用）
    /// </summary>
    [ContextMenu("开始打印")]
    public void TriggerPrint()
    {
        StartPrinting();
    }

    /// <summary>
    /// 手动停止所有动画和特效（用于调试或强制停止）
    /// </summary>
    [ContextMenu("强制停止")]
    public void ForceStop()
    {
        StopPrinting();

        if (enableDebugLog)
            Debug.Log("[PrinterSystem] 已强制停止所有动画和特效");
    }

    /// <summary>
    /// 检查当前状态（调试用）
    /// </summary>
    [ContextMenu("检查打印机状态")]
    public void CheckPrinterStatus()
    {
        Debug.Log($"[PrinterSystem] === 打印机状态检查 ===");
        Debug.Log($"是否正在打印: {isPrinting}");
        Debug.Log($"是否等待打印任务: {isWaitingForPrintJob}");
        Debug.Log($"生产时间设置: {productionTime}秒");

        if (printerAnimator != null)
        {
            bool isAnimating = printerAnimator.GetBool(ANIMATOR_IS_PRINTING);
            Debug.Log($"Animator状态: IsPrinting = {isAnimating}");
            Debug.Log($"当前动画状态: {printerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Shake")}");
        }
        else
        {
            Debug.Log("Animator组件未设置");
        }

        if (waitingJobText != null)
        {
            Debug.Log($"等待提示文本状态: {(waitingJobText.gameObject.activeInHierarchy ? "显示中" : "隐藏中")}");
        }
        else
        {
            Debug.Log("等待提示文本未设置");
        }

        Debug.Log($"任务道具预制件: {(taskItemPrefab != null ? taskItemPrefab.name : "未设置")}");
        Debug.Log($"生成位置: {(spawnPosition != null ? spawnPosition.name : "未设置")}");
    }

    /// <summary>
    /// 检查是否正在打印
    /// </summary>
    public bool IsPrinting => isPrinting;

    /// <summary>
    /// 测试等待状态切换（调试用）
    /// </summary>
    [ContextMenu("切换等待状态")]
    public void ToggleWaitingStatus()
    {
        SetWaitingForPrintJob(!isWaitingForPrintJob);
    }

    /// <summary>
    /// 获取或设置生产时间
    /// </summary>
    public float ProductionTime
    {
        get => productionTime;
        set => productionTime = Mathf.Max(0.1f, value); // 确保生产时间不小于0.1秒
    }

    void OnDrawGizmosSelected()
    {
        // 在Scene视图中显示生成位置
        if (spawnPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPosition.position, 0.1f);
            Gizmos.DrawLine(spawnPosition.position, spawnPosition.position + spawnPosition.forward * 0.3f);
        }
    }

    void OnDestroy()
    {
        // 确保在销毁时停止所有协程
        if (printCoroutine != null)
        {
            StopCoroutine(printCoroutine);
        }
    }
}