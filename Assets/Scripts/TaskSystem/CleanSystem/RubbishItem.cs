using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 垃圾物品组件 - 管理垃圾状态和属性
/// </summary>
public class RubbishItem : MonoBehaviour
{
    [Header("垃圾属性")]
    [SerializeField] private bool isInteractable = true; // 是否可交互

    [Header("生成信息")]
    [SerializeField] private Transform spawnPoint; // 生成点
    [SerializeField] private float spawnTime; // 生成时间

    [Header("状态信息")]
    [SerializeField] private bool isCleaned = false; // 是否已被清理
    [SerializeField] private bool isBeingCleaned = false; // 是否正在被清理

    [Header("调试设置")]
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
    }

    /// <summary>
    /// 初始化垃圾物品组件
    /// </summary>
    /// <param name="point">生成点</param>
    /// <param name="system">清理系统引用</param>
    public void Initialize(Transform point, SimplifiedCleanSystem system)
    {
        spawnPoint = point;
        cleanSystem = system;
        spawnTime = Time.time;

        // 确保标签正确
        if (!gameObject.CompareTag("Rubbish"))
        {
            gameObject.tag = "Rubbish";
        }

        // 设置为可交互
        SetInteractable(true);

        if (enableDebugLog)
            Debug.Log($"[RubbishItem] 垃圾 {name} 已初始化，生成点: {point?.name}");
    }

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        // 获取或添加必要组件
        rubbishRigidbody = GetComponent<Rigidbody>();
        if (rubbishRigidbody == null)
        {
            rubbishRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        rubbishCollider = GetComponent<Collider>();
        if (rubbishCollider == null)
        {
            // 如果没有碰撞体，添加一个Box Collider
            rubbishCollider = gameObject.AddComponent<BoxCollider>();
        }

        // 获取VR交互组件
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
        }

        // 绑定VR交互事件
        BindVRInteractionEvents();
    }

    /// <summary>
    /// 绑定VR交互事件
    /// </summary>
    private void BindVRInteractionEvents()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnPickedUp);
            grabInteractable.selectExited.AddListener(OnDropped);
        }
    }

    /// <summary>
    /// 解绑VR交互事件
    /// </summary>
    private void UnbindVRInteractionEvents()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnPickedUp);
            grabInteractable.selectExited.RemoveListener(OnDropped);
        }
    }

    /// <summary>
    /// 垃圾被拾起时调用
    /// </summary>
    /// <param name="args">选择事件参数</param>
    private void OnPickedUp(SelectEnterEventArgs args)
    {
        if (enableDebugLog)
            Debug.Log($"[RubbishItem] 垃圾 {name} 被拾起");

        OnRubbishPickedUp?.Invoke(this);
    }

    /// <summary>
    /// 垃圾被放下时调用
    /// </summary>
    /// <param name="args">选择退出事件参数</param>
    private void OnDropped(SelectExitEventArgs args)
    {
        if (enableDebugLog)
            Debug.Log($"[RubbishItem] 垃圾 {name} 被放下");

        OnRubbishDropped?.Invoke(this);
    }

    /// <summary>
    /// 设置可交互状态
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;

        if (grabInteractable != null)
        {
            grabInteractable.enabled = interactable;
        }

        if (enableDebugLog)
            Debug.Log($"[RubbishItem] 垃圾 {name} 交互状态设置为: {interactable}");
    }

    /// <summary>
    /// 标记为正在清理
    /// </summary>
    public void MarkAsBeingCleaned()
    {
        if (isBeingCleaned || isCleaned) return;

        isBeingCleaned = true;

        // 禁用交互
        SetInteractable(false);

        // 如果正在被抓取，强制释放
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            grabInteractable.interactionManager.SelectExit(
                grabInteractable.firstInteractorSelecting,
                grabInteractable
            );
        }

        if (enableDebugLog)
            Debug.Log($"[RubbishItem] 垃圾 {name} 标记为正在清理");
    }

    /// <summary>
    /// 标记为已清理
    /// </summary>
    public void MarkAsCleaned()
    {
        if (isCleaned) return;

        isCleaned = true;
        isBeingCleaned = false;

        // 改变标签
        gameObject.tag = "Untagged";

        // 彻底禁用交互
        SetInteractable(false);

        // 解绑事件
        UnbindVRInteractionEvents();

        // 触发清理事件
        OnRubbishCleaned?.Invoke(this);

        if (enableDebugLog)
            Debug.Log($"[RubbishItem] 垃圾 {name} 已标记为清理完成");
    }

    /// <summary>
    /// 获取垃圾存在时间
    /// </summary>
    /// <returns>存在时间（秒）</returns>
    public float GetExistenceTime()
    {
        return Time.time - spawnTime;
    }

    /// <summary>
    /// 检查是否可以被清理
    /// </summary>
    /// <returns>是否可以清理</returns>
    public bool CanBeCleaned()
    {
        return !isCleaned && !isBeingCleaned && gameObject.CompareTag("Rubbish");
    }

    /// <summary>
    /// 手动清理垃圾（调试用）
    /// </summary>
    [ContextMenu("手动清理")]
    public void ManualClean()
    {
        if (CanBeCleaned())
        {
            MarkAsBeingCleaned();
            MarkAsCleaned();

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
            Debug.Log($"[RubbishItem] 垃圾 {name} 无法被清理");
        }
    }

    /// <summary>
    /// 检查垃圾状态（调试用）
    /// </summary>
    [ContextMenu("检查状态")]
    public void CheckStatus()
    {
        Debug.Log($"[RubbishItem] === 垃圾 {name} 状态 ===");
        Debug.Log($"是否可交互: {isInteractable}");
        Debug.Log($"是否已清理: {isCleaned}");
        Debug.Log($"是否正在清理: {isBeingCleaned}");
        Debug.Log($"生成点: {(spawnPoint != null ? spawnPoint.name : "未设置")}");
        Debug.Log($"存在时间: {GetExistenceTime():F1}秒");
        Debug.Log($"当前标签: {tag}");
        Debug.Log($"是否可被清理: {CanBeCleaned()}");
        Debug.Log($"VR交互组件: {(grabInteractable != null ? "已设置" : "未设置")}");
        Debug.Log($"是否被抓取: {(grabInteractable != null ? grabInteractable.isSelected : false)}");
    }

    void OnDestroy()
    {
        // 解绑事件
        UnbindVRInteractionEvents();

        if (enableDebugLog)
            Debug.Log($"[RubbishItem] 垃圾 {name} 已销毁，存在时间: {GetExistenceTime():F1}秒");
    }

    // 属性访问器
    public bool IsInteractable => isInteractable;
    public bool IsCleaned => isCleaned;
    public bool IsBeingCleaned => isBeingCleaned;
    public Transform SpawnPoint => spawnPoint;
    public float SpawnTime => spawnTime;
    public SimplifiedCleanSystem CleanSystem => cleanSystem;
}