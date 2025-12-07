using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

/// <summary>
/// 垃圾物品组件 - 管理垃圾状态和属性
/// </summary>
public class RubbishItem : MonoBehaviour
{
    [Header("Rubbish Properties")]
    [SerializeField] private bool isInteractable = true; // 是否可交互

    [Header("Spawn Info")]
    [SerializeField] private Transform spawnPoint; // 生成点
    [SerializeField] private float spawnTime; // 生成时间

    [Header("Status Info")]
    [SerializeField] private bool isCleaned = false; // 是否已被清理
    [SerializeField] private bool isBeingCleaned = false; // 是否正在被清理

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLog = true; // 启用调试日志

    // 私有变量
    private SimplifiedCleanSystem cleanSystem; // 清理系统引用
    private XRGrabInteractable grabInteractable; // VR抓取组件
    private Rigidbody rubbishRigidbody; // 刚体组件
    private Collider rubbishCollider; // 碰撞体组件

    // 事件
    public System.Action<RubbishItem> OnRubbishCleaned; // 垃圾被清理事件
    public System.Action<RubbishItem> OnRubbishPickedUp; // 垃圾被拾起事件
    public System.Action<RubbishItem> OnRubbishDropped; // 垃圾被放下事件

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        spawnTime = Time.time;
        BindVRInteractionEvents();
        // 确保tag是正确的，便于垃圾桶识别
        gameObject.tag = "Rubbish";
    }

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rubbishRigidbody = GetComponent<Rigidbody>();
        rubbishCollider = GetComponent<Collider>();

        if (rubbishCollider != null)
        {
            rubbishCollider.isTrigger = false; // 确保是实体碰撞体
        }
    }

    /// <summary>
    /// 绑定VR交互事件
    /// </summary>
    private void BindVRInteractionEvents()
    {
        // ... (VR interaction binding logic)
    }

    /// <summary>
    /// 解绑VR交互事件
    /// </summary>
    private void UnbindVRInteractionEvents()
    {
        // ... (VR interaction unbinding logic)
    }

    // ... (OnPickedUp/OnDropped methods)

    /// <summary>
    /// 设置清理系统引用
    /// </summary>
    public void SetCleanSystem(SimplifiedCleanSystem system)
    {
        cleanSystem = system;
    }

    /// <summary>
    /// 设置生成点
    /// </summary>
    public void SetSpawnPoint(Transform point)
    {
        spawnPoint = point;
    }

    /// <summary>
    /// 获取物品存在时间
    /// </summary>
    public float GetExistenceTime()
    {
        return Time.time - spawnTime;
    }

    /// <summary>
    /// 属性访问器：生成点
    /// </summary>
    public Transform SpawnPoint => spawnPoint;


    /// <summary>
    /// 检查是否可以被清理
    /// </summary>
    public bool CanBeCleaned()
    {
        return !isCleaned && isInteractable;
    }

    /// <summary>
    /// 尝试清理物品（在垃圾桶触发器中调用）
    /// </summary>
    public void TryClean()
    {
        if (!CanBeCleaned())
        {
            if (enableDebugLog) Debug.Log($"[RubbishItem] Rubbish {name} is already cleaned or not interactable.");
            return;
        }

        if (enableDebugLog) Debug.Log($"[RubbishItem] Attempting to clean rubbish {name}...");

        isBeingCleaned = true;

        // 如果正在被抓取，强制释放
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            // grabInteractable.interactionManager.SelectExit(
            //     grabInteractable.firstInteractorSelecting, grabInteractable
            // );
        }

        // 禁用物理和交互
        if (rubbishRigidbody != null) rubbishRigidbody.isKinematic = true;
        if (rubbishCollider != null) rubbishCollider.enabled = false;
        if (grabInteractable != null) grabInteractable.enabled = false;

        // 标记为已清理
        isCleaned = true;

        // 延迟调用清理事件，以允许垃圾桶特效/音效播放
        StartCoroutine(CleanupRoutine());
    }

    /// <summary>
    /// 清理协程
    /// </summary>
    private IEnumerator CleanupRoutine()
    {
        // 简单等待 0.5 秒
        yield return new WaitForSeconds(0.5f);

        FinalizeClean();
    }

    /// <summary>
    /// 最终清理逻辑
    /// </summary>
    private void FinalizeClean()
    {
        if (isCleaned)
        {
            // 触发事件
            OnRubbishCleaned?.Invoke(this);

            // 通知清理系统
            if (cleanSystem != null)
            {
                cleanSystem.OnRubbishCleanedCallback(gameObject);
            }

            // 销毁对象
            Destroy(gameObject, 0.5f);
        }
        else
        {
            if (enableDebugLog) Debug.Log($"[RubbishItem] Rubbish {name} cannot be cleaned");
        }
    }

    /// <summary>
    /// 检查垃圾状态（调试用）
    /// </summary>
    [ContextMenu("Check Status")]
    public void CheckStatus()
    {
        Debug.Log($"[RubbishItem] === Rubbish {name} Status ===");
        Debug.Log($"Is Interactable: {isInteractable}");
        Debug.Log($"Is Cleaned: {isCleaned}");
        Debug.Log($"Is Being Cleaned: {isBeingCleaned}");
        Debug.Log($"Spawn Point: {(spawnPoint != null ? spawnPoint.name : "Not Set")}");
        Debug.Log($"Existence Time: {GetExistenceTime():F1}s");
        Debug.Log($"Current Tag: {tag}");
        Debug.Log($"Can Be Cleaned: {CanBeCleaned()}");
        // Debug.Log($"VR Interactor Component: {(grabInteractable != null ? "Set" : "Not Set")}");
        // Debug.Log($"Is Grabbed: {(grabInteractable != null ? grabInteractable.isSelected : false)}");
    }

    void OnDestroy()
    {
        // 解绑事件
        UnbindVRInteractionEvents();

        if (enableDebugLog)
        {
            // Debug.Log($"[RubbishItem] Rubbish {name} destroyed.");
        }
    }
}