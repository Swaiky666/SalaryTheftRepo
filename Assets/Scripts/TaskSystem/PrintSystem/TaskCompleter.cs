using UnityEngine;
using TMPro;

/// <summary>
/// 任务完成器组件
/// 负责检测任务道具并完成任务
/// </summary>
public class TaskCompleter : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private string requiredItemTag = "TaskMaterial"; // 需要的物品标签
    [SerializeField] private int taskIndex = -1; // 对应的任务索引
    [SerializeField] private bool enableDebugLog = true; // 启用调试日志

    // 私有变量
    private PrintTaskHandler taskHandler; // 任务处理器引用
    private bool isInitialized = false; // 是否已初始化
    private Collider triggerCollider; // 触发器碰撞体
    private TextMeshProUGUI taskDescriptionText; // 任务描述文本（自动查找）

    void Start()
    {
        // 确保有触发器碰撞体
        SetupTriggerCollider();

        // 自动查找Text组件
        FindTextComponent();
    }

    /// <summary>
    /// 初始化任务完成器
    /// </summary>
    /// <param name="itemTag">需要的物品标签</param>
    /// <param name="index">任务索引</param>
    /// <param name="handler">任务处理器引用</param>
    /// <param name="displayText">显示的任务描述文本</param>
    public void Initialize(string itemTag, int index, PrintTaskHandler handler, string displayText)
    {
        requiredItemTag = itemTag;
        taskIndex = index;
        taskHandler = handler;
        isInitialized = true;

        // 确保找到Text组件
        if (taskDescriptionText == null)
        {
            FindTextComponent();
        }

        // 设置任务描述文本
        SetTaskDescriptionText(displayText);

        if (enableDebugLog)
            Debug.Log($"[TaskCompleter] 已初始化 - 任务索引: {taskIndex}, 需要物品标签: {requiredItemTag}, 显示文本: {displayText}");
    }

    /// <summary>
    /// 自动查找Text组件
    /// </summary>
    private void FindTextComponent()
    {
        // 在子对象中查找TextMeshProUGUI组件
        taskDescriptionText = GetComponentInChildren<TextMeshProUGUI>();

        if (taskDescriptionText != null)
        {
            if (enableDebugLog)
                Debug.Log($"[TaskCompleter] 自动找到了TextMeshProUGUI组件: {taskDescriptionText.name}");
        }
        else
        {
            Debug.LogWarning("[TaskCompleter] 未找到TextMeshProUGUI组件，任务描述将无法显示");
        }
    }

    /// <summary>
    /// 设置任务描述文本
    /// </summary>
    /// <param name="displayText">要显示的文本</param>
    private void SetTaskDescriptionText(string displayText)
    {
        if (taskDescriptionText != null)
        {
            taskDescriptionText.text = displayText;

            if (enableDebugLog)
                Debug.Log($"[TaskCompleter] 任务描述文本已设置为: {displayText}");
        }
        else
        {
            Debug.LogWarning("[TaskCompleter] 无法设置任务描述文本，未找到TextMeshProUGUI组件");
        }
    }

    /// <summary>
    /// 设置触发器碰撞体
    /// </summary>
    private void SetupTriggerCollider()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null)
        {
            // 如果没有碰撞体，添加一个
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            if (enableDebugLog)
                Debug.Log("[TaskCompleter] 自动添加了BoxCollider组件");
        }

        // 确保是触发器
        if (!triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
            if (enableDebugLog)
                Debug.Log("[TaskCompleter] 已设置碰撞体为触发器");
        }
    }

    /// <summary>
    /// 触发器进入事件
    /// </summary>
    /// <param name="other">进入的碰撞体</param>
    void OnTriggerEnter(Collider other)
    {
        if (!isInitialized)
        {
            if (enableDebugLog)
                Debug.LogWarning("[TaskCompleter] 未初始化，忽略触发事件");
            return;
        }

        if (taskHandler == null)
        {
            Debug.LogError("[TaskCompleter] TaskHandler引用为空");
            return;
        }

        // 检查物品标签
        if (other.CompareTag(requiredItemTag))
        {
            if (enableDebugLog)
                Debug.Log($"[TaskCompleter] ✅ 检测到正确的任务道具: {other.name} (标签: {requiredItemTag})");

            // 销毁任务道具
            Destroy(other.gameObject);

            // 通知任务处理器任务完成
            taskHandler.OnTaskCompleted(taskIndex, gameObject);
        }
        else
        {
            if (enableDebugLog)
                Debug.Log($"[TaskCompleter] ❌ 物品标签不匹配: 需要 '{requiredItemTag}', 实际 '{other.tag}'");
        }
    }

    /// <summary>
    /// 手动完成任务（调试用）
    /// </summary>
    [ContextMenu("手动完成任务")]
    public void ManualCompleteTask()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[TaskCompleter] 未初始化，无法手动完成任务");
            return;
        }

        if (taskHandler == null)
        {
            Debug.LogError("[TaskCompleter] TaskHandler引用为空");
            return;
        }

        taskHandler.OnTaskCompleted(taskIndex, gameObject);

        if (enableDebugLog)
            Debug.Log("[TaskCompleter] 手动完成任务");
    }

    /// <summary>
    /// 检查任务完成器状态（调试用）
    /// </summary>
    [ContextMenu("检查状态")]
    public void CheckStatus()
    {
        Debug.Log($"[TaskCompleter] === 任务完成器状态 ===");
        Debug.Log($"是否已初始化: {isInitialized}");
        Debug.Log($"任务索引: {taskIndex}");
        Debug.Log($"需要物品标签: {requiredItemTag}");
        Debug.Log($"TaskHandler引用: {(taskHandler != null ? "已设置" : "未设置")}");
        Debug.Log($"触发器碰撞体: {(triggerCollider != null ? "已设置" : "未设置")}");
        Debug.Log($"任务描述文本组件: {(taskDescriptionText != null ? "已设置" : "未设置")}");

        if (triggerCollider != null)
        {
            Debug.Log($"碰撞体类型: {triggerCollider.GetType().Name}");
            Debug.Log($"是否为触发器: {triggerCollider.isTrigger}");
        }

        if (taskDescriptionText != null)
        {
            Debug.Log($"当前显示文本: {taskDescriptionText.text}");
        }
    }

    /// <summary>
    /// 测试设置文本（调试用）
    /// </summary>
    [ContextMenu("测试设置文本")]
    public void TestSetText()
    {
        SetTaskDescriptionText("Need Manual");
    }

    /// <summary>
    /// 在Scene视图中显示触发区域
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = isInitialized ? Color.green : Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                Gizmos.DrawWireCube(capsule.center, new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2));
            }
        }
    }

    void OnDestroy()
    {
        if (enableDebugLog)
            Debug.Log($"[TaskCompleter] 任务完成器已销毁 - 任务索引: {taskIndex}");
    }
}