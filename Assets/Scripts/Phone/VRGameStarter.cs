using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 通过 XR Grab Interactable 的抓取事件来控制 ClimbingGameUI 的启动和停止。
/// </summary>
[RequireComponent(typeof(ClimbingGameUI))]
public class VRGameStarter : MonoBehaviour
{
    [Tooltip("场景中可交互的组件 (如 XRGrabInteractable 或 XRSimpleInteractable)。")]
    // 注意：在 Inspector 中必须指定这个组件！
    [SerializeField]
    private XRBaseInteractable interactable;

    private ClimbingGameUI climbingGameUI;

    private void Start()
    {
        climbingGameUI = GetComponent<ClimbingGameUI>();

        if (interactable == null)
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (interactable == null)
            {
                Debug.LogError("[VRGameStarter] 找不到 XRBaseInteractable 组件。请确保'手机'物体上挂载了 XRGrabInteractable/XRSimpleInteractable，并在 Inspector 中指定或让本脚本自动查找！");
                return;
            }
        }

        // 注册抓取/选择开始和结束事件
        // 当手柄“抓取”物体时，StartGame()
        interactable.selectEntered.AddListener(OnGrabStart);
        // 当手柄“释放”物体时，StopGame()
        interactable.selectExited.AddListener(OnGrabEnd);

        Debug.Log("[VRGameStarter] 初始化完成。已监听 XR 抓取事件。");
    }

    private void OnDestroy()
    {
        // 移除监听器，防止内存泄漏和空引用错误
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnGrabStart);
            interactable.selectExited.RemoveListener(OnGrabEnd);
        }
    }

    /// <summary>
    /// 当手柄开始抓取时调用，启动游戏。
    /// </summary>
    /// <param name="interactor">触发抓取的交互器</param>
    private void OnGrabStart(SelectEnterEventArgs args)
    {
        Debug.Log("[VRGameStarter] 检测到抓取开始。调用 StartGame()");
        climbingGameUI.StartGame();
    }

    /// <summary>
    /// 当手柄释放抓取时调用，停止游戏。
    /// </summary>
    /// <param name="interactor">触发释放的交互器</param>
    private void OnGrabEnd(SelectExitEventArgs args)
    {
        Debug.Log("[VRGameStarter] 检测到抓取结束。调用 StopGame()");
        climbingGameUI.StopGame();
    }
}