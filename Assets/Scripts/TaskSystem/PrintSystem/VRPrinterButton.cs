using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VR打印机按钮控制器
/// 处理VR环境中的打印机按钮交互
/// </summary>
public class VRPrinterButton : MonoBehaviour
{
    [Header("打印机设置")]
    [SerializeField] private PrinterSystem printerSystem; // 打印机系统引用
    [SerializeField] private TaskManager taskManager; // 任务管理器引用

    [Header("交互设置")]
    [SerializeField] private XRSimpleInteractable buttonInteractable; // XR交互组件
    [SerializeField] private Transform buttonTransform; // 按钮变换组件（用于视觉反馈）

    [Header("视觉反馈")]
    [SerializeField] private Vector3 pressedPosition = new Vector3(0, -0.01f, 0); // 按下时的位置偏移
    [SerializeField] private Color normalColor = Color.white; // 正常颜色
    [SerializeField] private Color pressedColor = Color.green; // 按下时的颜色
    [SerializeField] private Color disabledColor = Color.gray; // 禁用时的颜色
    [SerializeField] private Renderer buttonRenderer; // 按钮渲染器

    [Header("音频反馈")]
    [SerializeField] private AudioSource audioSource; // 音频源
    [SerializeField] private AudioClip buttonPressSound; // 按钮按下音效
    [SerializeField] private AudioClip buttonErrorSound; // 按钮错误音效

    [Header("触觉反馈")]
    [SerializeField] private float hapticIntensity = 0.5f; // 触觉反馈强度
    [SerializeField] private float hapticDuration = 0.1f; // 触觉反馈持续时间

    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true; // 启用调试日志

    // 私有变量
    private Vector3 originalPosition; // 原始位置
    private Material buttonMaterial; // 按钮材质
    private bool isPressed = false; // 是否正在按下
    private bool canInteract = true; // 是否可以交互

    void Start()
    {
        InitializeButton();
    }

    /// <summary>
    /// 初始化按钮
    /// </summary>
    private void InitializeButton()
    {
        // 获取或创建必要组件
        if (buttonInteractable == null)
            buttonInteractable = GetComponent<XRSimpleInteractable>();

        if (buttonTransform == null)
            buttonTransform = transform;

        if (buttonRenderer == null)
            buttonRenderer = GetComponent<Renderer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // 记录原始位置
        originalPosition = buttonTransform.localPosition;

        // 设置按钮材质
        if (buttonRenderer != null)
        {
            buttonMaterial = buttonRenderer.material;
            SetButtonColor(normalColor);
        }

        // 绑定交互事件
        if (buttonInteractable != null)
        {
            // 推荐使用 Select Events
            buttonInteractable.selectEntered.AddListener(OnButtonPressed);
            buttonInteractable.selectExited.AddListener(OnButtonReleased);

            // 可选：添加 Hover Events 用于悬停效果
            buttonInteractable.hoverEntered.AddListener(OnButtonHover);
            buttonInteractable.hoverExited.AddListener(OnButtonHoverExit);
        }

        if (enableDebugLog)
            Debug.Log("[VRPrinterButton] VR打印机按钮已初始化");
    }

    /// <summary>
    /// 按钮被按下时调用
    /// </summary>
    /// <param name="args">选择事件参数</param>
    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        if (!canInteract || isPressed)
            return;

        isPressed = true;

        if (enableDebugLog)
            Debug.Log("[VRPrinterButton] 按钮被按下");

        // 检查是否可以打印
        if (CanPrint())
        {
            // 执行打印操作
            ExecutePrint();

            // 视觉反馈
            SetButtonPressed(true);
            SetButtonColor(pressedColor);

            // 音频反馈
            PlaySound(buttonPressSound);

            // 触觉反馈
            SendHapticFeedback(args.interactorObject);
        }
        else
        {
            // 无法打印时的反馈
            SetButtonColor(disabledColor);
            PlaySound(buttonErrorSound);

            if (enableDebugLog)
                Debug.Log("[VRPrinterButton] 无法打印：没有等待的打印任务");
        }
    }

    /// <summary>
    /// 按钮释放时调用
    /// </summary>
    /// <param name="args">选择退出事件参数</param>
    private void OnButtonReleased(SelectExitEventArgs args)
    {
        if (!isPressed)
            return;

        isPressed = false;

        if (enableDebugLog)
            Debug.Log("[VRPrinterButton] 按钮释放");

        // 恢复按钮状态
        SetButtonPressed(false);
        SetButtonColor(normalColor);
    }

    /// <summary>
    /// 鼠标悬停进入
    /// </summary>
    /// <param name="args">悬停进入事件参数</param>
    private void OnButtonHover(HoverEnterEventArgs args)
    {
        if (!canInteract)
            return;

        // 悬停时的轻微高亮效果
        if (!isPressed)
        {
            Color hoverColor = Color.Lerp(normalColor, pressedColor, 0.3f);
            SetButtonColor(hoverColor);
        }

        if (enableDebugLog)
            Debug.Log("[VRPrinterButton] 悬停进入");
    }

    /// <summary>
    /// 鼠标悬停退出
    /// </summary>
    /// <param name="args">悬停退出事件参数</param>
    private void OnButtonHoverExit(HoverExitEventArgs args)
    {
        if (!canInteract)
            return;

        // 恢复正常颜色
        if (!isPressed)
        {
            SetButtonColor(normalColor);
        }

        if (enableDebugLog)
            Debug.Log("[VRPrinterButton] 悬停退出");
    }

    /// <summary>
    /// 检查是否可以打印
    /// </summary>
    /// <returns>是否可以打印</returns>
    private bool CanPrint()
    {
        // 检查打印机系统是否存在且等待打印任务
        if (printerSystem == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("[VRPrinterButton] 打印机系统引用为空");
            return false;
        }

        // 这里可以添加更多检查逻辑
        // 例如：检查是否有纸张、墨水等
        return true;
    }

    /// <summary>
    /// 执行打印操作
    /// </summary>
    private void ExecutePrint()
    {
        if (printerSystem != null)
        {
            // 触发打印机系统的打印操作
            // 这里可能需要根据你的PrinterSystem的具体实现来调整
            printerSystem.StartPrinting();

            if (enableDebugLog)
                Debug.Log("[VRPrinterButton] 打印操作已执行");
        }

        if (taskManager != null)
        {
            // 如果需要通知任务管理器
            // taskManager.OnPrintButtonPressed();
        }
    }

    /// <summary>
    /// 设置按钮按下状态
    /// </summary>
    /// <param name="pressed">是否按下</param>
    private void SetButtonPressed(bool pressed)
    {
        if (buttonTransform != null)
        {
            Vector3 targetPosition = pressed ? originalPosition + pressedPosition : originalPosition;
            buttonTransform.localPosition = targetPosition;
        }
    }

    /// <summary>
    /// 设置按钮颜色
    /// </summary>
    /// <param name="color">目标颜色</param>
    private void SetButtonColor(Color color)
    {
        if (buttonMaterial != null)
        {
            buttonMaterial.color = color;
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 发送触觉反馈
    /// </summary>
    /// <param name="interactor">交互器对象</param>
    private void SendHapticFeedback(IXRInteractor interactor)
    {
        if (interactor is XRBaseControllerInteractor controllerInteractor)
        {
            // 发送触觉反馈到控制器
            if (controllerInteractor.xrController is ActionBasedController actionController)
            {
                actionController.SendHapticImpulse(hapticIntensity, hapticDuration);
            }
            else if (controllerInteractor.xrController is XRController xrController)
            {
                xrController.SendHapticImpulse(hapticIntensity, hapticDuration);
            }
        }
    }

    /// <summary>
    /// 设置按钮交互状态
    /// </summary>
    /// <param name="canInteract">是否可以交互</param>
    public void SetInteractable(bool canInteract)
    {
        this.canInteract = canInteract;

        if (buttonInteractable != null)
        {
            buttonInteractable.enabled = canInteract;
        }

        // 更新视觉状态
        Color targetColor = canInteract ? normalColor : disabledColor;
        SetButtonColor(targetColor);

        if (enableDebugLog)
            Debug.Log($"[VRPrinterButton] 按钮交互状态设置为: {canInteract}");
    }

    /// <summary>
    /// 手动触发打印（用于测试）
    /// </summary>
    [ContextMenu("手动触发打印")]
    public void ManualTriggerPrint()
    {
        if (CanPrint())
        {
            ExecutePrint();
            Debug.Log("[VRPrinterButton] 手动触发打印成功");
        }
        else
        {
            Debug.Log("[VRPrinterButton] 手动触发打印失败：无法打印");
        }
    }

    /// <summary>
    /// 检查按钮状态（调试用）
    /// </summary>
    [ContextMenu("检查按钮状态")]
    public void CheckButtonStatus()
    {
        Debug.Log($"[VRPrinterButton] === 按钮状态检查 ===");
        Debug.Log($"是否可以交互: {canInteract}");
        Debug.Log($"是否正在按下: {isPressed}");
        Debug.Log($"打印机系统引用: {(printerSystem != null ? "已设置" : "未设置")}");
        Debug.Log($"任务管理器引用: {(taskManager != null ? "已设置" : "未设置")}");
        Debug.Log($"XR交互组件: {(buttonInteractable != null ? "已设置" : "未设置")}");
        Debug.Log($"是否可以打印: {CanPrint()}");
    }

    void OnDestroy()
    {
        // 清理事件监听
        if (buttonInteractable != null)
        {
            buttonInteractable.selectEntered.RemoveListener(OnButtonPressed);
            buttonInteractable.selectExited.RemoveListener(OnButtonReleased);
            buttonInteractable.hoverEntered.RemoveListener(OnButtonHover);
            buttonInteractable.hoverExited.RemoveListener(OnButtonHoverExit);
        }
    }
}