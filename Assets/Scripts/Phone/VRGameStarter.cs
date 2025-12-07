using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 通过 XR Grab Interactable 的抓取事件来控制 ClimbingGameUI 的启动和停止，
/// 并添加持续的压力缓解功能。
/// </summary>
[RequireComponent(typeof(ClimbingGameUI))]
public class VRGameStarter : MonoBehaviour
{
    [Header("VR 交互引用")]
    [Tooltip("场景中可交互的组件 (如 XRGrabInteractable)。")]
    [SerializeField]
    private XRBaseInteractable interactable;

    [Header("压力缓解设置")]
    [Tooltip("每秒缓解的压力值 (例如 5f 表示每秒降低 5 点压力)")]
    [SerializeField]
    private float stressReductionRate = 5f; // 可以调整这个值来控制下降速度

    // 内部引用
    private ClimbingGameUI climbingGameUI;
    // 直接引用 GameLogicSystem，用于修改压力值
    private GameLogicSystem gameLogicSystem;

    // 状态跟踪
    private bool isBeingGrabbed = false;

    private void Start()
    {
        climbingGameUI = GetComponent<ClimbingGameUI>();

        // 查找 GameLogicSystem 组件
        gameLogicSystem = FindObjectOfType<GameLogicSystem>();
        if (gameLogicSystem == null)
        {
            // 提示：如果找不到这个系统，压力缓解功能将失效。
            Debug.LogError("[VRGameStarter] 找不到场景中的 GameLogicSystem 组件，压力缓解功能将失效！");
        }

        if (interactable == null)
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (interactable == null)
            {
                Debug.LogError("[VRGameStarter] 找不到 XRBaseInteractable 组件。请确保'手机'物体上挂载了 XRGrabInteractable/XRSimpleInteractable！");
                return;
            }
        }

        // 注册抓取/选择开始和结束事件
        interactable.selectEntered.AddListener(OnGrabStart);
        interactable.selectExited.AddListener(OnGrabEnd);

        Debug.Log("[VRGameStarter] 初始化完成。已监听 XR 抓取事件。");
    }

    private void Update()
    {
        // 只有当被抓取且找到了 GameLogicSystem 时才持续缓解压力
        if (isBeingGrabbed && gameLogicSystem != null)
        {
            // 计算本帧需要减少的压力值：(每秒速率 * 帧时间)
            float reductionAmount = stressReductionRate * Time.deltaTime;

            // 调用 GameLogicSystem 中的 ReduceStress 方法来缓慢降低压力值
            // GameLogicSystem 内部会处理边界值，确保压力不会低于 0。
            gameLogicSystem.ReduceStress(reductionAmount);
        }
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnGrabStart);
            interactable.selectExited.RemoveListener(OnGrabEnd);
        }
    }

    private void OnGrabStart(SelectEnterEventArgs args)
    {
        Debug.Log("[VRGameStarter] 检测到抓取开始。调用 StartGame()");
        isBeingGrabbed = true; // 设置状态为正在抓取
        climbingGameUI.StartGame();
    }

    private void OnGrabEnd(SelectExitEventArgs args)
    {
        Debug.Log("[VRGameStarter] 检测到抓取结束。调用 StopGame()");
        isBeingGrabbed = false; // 设置状态为停止抓取
        climbingGameUI.StopGame();
    }
}