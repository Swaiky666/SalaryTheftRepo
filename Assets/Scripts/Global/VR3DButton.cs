using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 通用 3D VR 按钮控制器 (适用于 Cube/Mesh)
/// 处理 XR Interaction Toolkit 的 Select/Hover 事件，并提供视觉、音频和触觉反馈。
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class VR3DButton : MonoBehaviour
{
    [Header("核心功能")]
    [Tooltip("按钮被成功选择（按下）时触发的事件")]
    public UnityEvent OnClicked = new UnityEvent();

    [Header("XR 交互组件")]
    [SerializeField] private XRSimpleInteractable buttonInteractable;
    [SerializeField] private Transform buttonTransform;

    [Header("视觉反馈")]
    [SerializeField] private Vector3 pressedPositionOffset = new Vector3(0, -0.01f, 0);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color pressedColor = Color.green;
    [SerializeField] private Color disabledColor = Color.gray;
    [SerializeField] private Renderer buttonRenderer;

    [Header("音频反馈")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField][Range(0f, 1f)] private float hoverVolume = 0.5f;

    [Header("触觉反馈")]
    [SerializeField] private float hapticIntensity = 0.5f;
    [SerializeField] private float hapticDuration = 0.1f;

    // 私有变量
    private Vector3 originalPosition;
    private Material buttonMaterial;
    private bool isPressed = false;
    private bool canInteract = true; // 借鉴 VRPrinterButton 的状态管理

    void Start()
    {
        InitializeButton();
    }

    /// <summary>
    /// 初始化按钮
    /// </summary>
    private void InitializeButton()
    {
        // 自动获取组件
        if (buttonInteractable == null)
            buttonInteractable = GetComponent<XRSimpleInteractable>();

        if (buttonTransform == null)
            buttonTransform = transform;

        if (buttonRenderer == null)
            buttonRenderer = GetComponent<Renderer>();

        // 记录原始位置
        originalPosition = buttonTransform.localPosition;

        // 设置按钮材质和初始颜色
        if (buttonRenderer != null)
        {
            buttonMaterial = buttonRenderer.material;
            SetButtonColor(normalColor);
            Debug.Log($"[VR3DButton:{gameObject.name}] 材质已尝试初始化。");
        }
        else
        {
            Debug.LogError($"[VR3DButton:{gameObject.name}] Renderer组件为空! 无法获取材质。");
        }

        // 绑定交互事件
        if (buttonInteractable != null)
        {
            buttonInteractable.selectEntered.AddListener(OnButtonPressed);
            buttonInteractable.selectExited.AddListener(OnButtonReleased);
            buttonInteractable.hoverEntered.AddListener(OnButtonHover);
            buttonInteractable.hoverExited.AddListener(OnButtonHoverExit);
            Debug.Log($"[VR3DButton:{gameObject.name}] 事件监听已绑定。");
        }
        else
        {
            Debug.LogError("[VR3DButton] 缺少 XRSimpleInteractable 组件！");
        }
    }

    /// <summary>
    /// 按钮被按下时调用 (无论是射线点击还是直接触摸)
    /// </summary>
    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        Debug.Log($"[VR3DButton:{gameObject.name}] OnButtonPressed 尝试触发。");

        if (!canInteract)
        {
            Debug.LogWarning($"[VR3DButton:{gameObject.name}] 按钮被禁用 (canInteract=false)，阻止操作。");
            return;
        }

        if (isPressed)
        {
            Debug.LogWarning($"[VR3DButton:{gameObject.name}] 按钮已在按下状态 (isPressed=true)，阻止操作。");
            return;
        }

        isPressed = true;
        Debug.Log($"[VR3DButton:{gameObject.name}] 成功进入按下状态，执行反馈。");

        // 视觉反馈：按下位置和颜色
        SetButtonPressed(true);
        SetButtonColor(pressedColor);

        // 触觉反馈
        SendHapticFeedback(args.interactorObject);
    }

    /// <summary>
    /// 按钮释放时调用
    /// </summary>
    private void OnButtonReleased(SelectExitEventArgs args)
    {
        Debug.Log($"[VR3DButton:{gameObject.name}] OnButtonReleased 触发。");

        if (!isPressed)
        {
            Debug.LogWarning($"[VR3DButton:{gameObject.name}] 释放时isPressed为false，忽略。");
            return;
        }

        isPressed = false;

        // 触发外部绑定的点击事件
        OnClicked.Invoke();
        Debug.Log($"[VR3DButton:{gameObject.name}] OnClicked 事件触发。");

        // 恢复按钮状态
        SetButtonPressed(false);
        SetButtonColor(normalColor);
    }

    /// <summary>
    /// VR 射线或触碰悬停进入
    /// </summary>
    private void OnButtonHover(HoverEnterEventArgs args)
    {
        Debug.Log($"[VR3DButton:{gameObject.name}] OnButtonHover 触发。");

        if (!canInteract) return;

        // 悬停时的颜色变化
        if (!isPressed)
        {
            SetButtonColor(hoverColor);
        }

        // 播放悬停音效
        if (AudioManager.Instance != null && hoverSound != null)
        {
            AudioManager.Instance.PlaySFX(hoverSound, hoverVolume);
        }
    }

    /// <summary>
    /// VR 射线或触碰悬停退出
    /// </summary>
    private void OnButtonHoverExit(HoverExitEventArgs args)
    {
        Debug.Log($"[VR3DButton:{gameObject.name}] OnButtonHoverExit 触发。");

        if (!canInteract) return;

        // 恢复正常颜色
        if (!isPressed)
        {
            SetButtonColor(normalColor);
        }
    }

    // ... [SetButtonPressed, SendHapticFeedback 等方法保持不变]

    /// <summary>
    /// 设置按钮颜色 (包含材质检查)
    /// </summary>
    private void SetButtonColor(Color color)
    {
        if (buttonMaterial != null)
        {
            Debug.Log($"[VR3DButton:{gameObject.name}] 尝试设置颜色: {color}");
            // 兼容 URP/HDRP 或 Standard Shader
            if (buttonMaterial.HasProperty("_BaseColor"))
            {
                buttonMaterial.SetColor("_BaseColor", color);
            }
            else if (buttonMaterial.HasProperty("_Color"))
            {
                buttonMaterial.SetColor("_Color", color);
            }
            else
            {
                buttonMaterial.color = color;
            }
        }
        else
        {
            Debug.LogError($"[VR3DButton:{gameObject.name}] 材质为空 (buttonMaterial is null)! 无法设置颜色。");
        }
    }

    /// <summary>
    /// 设置按钮交互状态 (借鉴 VRPrinterButton 的状态管理)
    /// </summary>
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
        Debug.Log($"[VR3DButton:{gameObject.name}] 交互状态设置为: {canInteract}");
    }

    private void SetButtonPressed(bool pressed)
    {
        if (buttonTransform != null)
        {
            Vector3 targetPosition = pressed ? originalPosition + pressedPositionOffset : originalPosition;
            buttonTransform.localPosition = targetPosition;
        }
    }

    private void SendHapticFeedback(IXRInteractor interactor)
    {
        if (interactor is XRBaseControllerInteractor controllerInteractor)
        {
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