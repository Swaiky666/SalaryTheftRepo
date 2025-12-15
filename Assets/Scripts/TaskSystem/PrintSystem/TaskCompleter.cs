using UnityEngine;
using TMPro;
using System.Collections; // Required for IEnumerator and WaitForSeconds

/// <summary>
/// Task Completer Component.
/// Responsible for detecting required task items and completing the task.
/// </summary>
public class TaskCompleter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string requiredItemTag = "TaskMaterial"; // Tag of the item required to complete the task
    [SerializeField] private int taskIndex = -1; // Corresponding task index
    [SerializeField] private bool enableDebugLog = true; // Enables debug logs

    // Reference for the Particle System attached to this GameObject
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem completionParticle; // Particle system to play upon task completion

    /// <summary>
    /// Public property to access the task index for PrintTaskHandler.
    /// </summary>
    public int TaskIndex => taskIndex;

    // Private variables
    private PrintTaskHandler taskHandler; // Task handler reference (used for type checking, though TaskManager is the callback target)
    private TaskManager taskManager; // [New/Implicit] TaskManager reference (if TaskHandler passes it down)
    private bool isInitialized = false; // Flag indicating if initialization is complete
    private Collider triggerCollider; // Reference to the trigger collider
    private TextMeshProUGUI taskDescriptionText; // Task description text component (automatically searched)
    private bool isTaskFinished = false; // Flag to prevent duplicate task completion

    void Start()
    {
        // Ensure there is a trigger collider
        SetupTriggerCollider();

        // Automatically find the Text component
        FindTextComponent();
        
        // Automatically find the Particle System if not manually set
        if (completionParticle == null)
        {
            completionParticle = GetComponent<ParticleSystem>();
            if (completionParticle == null && enableDebugLog)
                Debug.LogWarning("[TaskCompleter] ParticleSystem component not found on this GameObject. No particle effect will play before destruction.");
        }
    }

    /// <summary>
    /// Initializes the Task Completer with required settings.
    /// </summary>
    /// <param name="itemTag">The tag of the required item.</param>
    /// <param name="index">The task index in TaskManager's active list.</param>
    /// <param name="handler">Reference to the Print Task Handler (caller).</param>
    /// <param name="displayText">The display text for the task description.</param>
    public void Initialize(string itemTag, int index, PrintTaskHandler handler, string displayText)
    {
        requiredItemTag = itemTag;
        taskIndex = index;
        taskHandler = handler;
        // Assuming PrintTaskHandler has a way to reference TaskManager, or we find it.
        // For simplicity and assuming TaskManager is the core system:
        if (taskManager == null)
        {
            taskManager = FindObjectOfType<TaskManager>();
        }

        isInitialized = true;

        // Ensure the Text component is found
        if (taskDescriptionText == null)
        {
            FindTextComponent();
        }

        // Set the task description text
        SetTaskDescriptionText(displayText);

        if (enableDebugLog)
            Debug.Log($"[TaskCompleter] Initialized - Task Index: {taskIndex}, Required Item Tag: {requiredItemTag}, Display Text: {displayText}");
    }

    /// <summary>
    /// Automatically searches for the TextMeshProUGUI component in children.
    /// </summary>
    private void FindTextComponent()
    {
        // Search for TextMeshProUGUI component in children
        taskDescriptionText = GetComponentInChildren<TextMeshProUGUI>();

        if (taskDescriptionText == null)
        {
            Debug.LogWarning("[TaskCompleter] TextMeshProUGUI component not found, task description will not be displayed.");
        }
    }

    /// <summary>
    /// Sets the task description text.
    /// </summary>
    /// <param name="displayText">The text to display.</param>
    private void SetTaskDescriptionText(string displayText)
    {
        if (taskDescriptionText != null)
        {
            taskDescriptionText.text = displayText;
        }
    }

    /// <summary>
    /// Sets up the trigger collider. Adds a BoxCollider if none exists and ensures it's a trigger.
    /// </summary>
    private void SetupTriggerCollider()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
        }

        if (!triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// Trigger Enter Event.
    /// </summary>
    /// <param name="other">The entering collider.</param>
    void OnTriggerEnter(Collider other)
    {
        if (!isInitialized || isTaskFinished || taskManager == null)
        {
            if (enableDebugLog) 
                Debug.LogWarning("[TaskCompleter] Not ready, manager is null, or already finished, ignoring trigger.");
            return;
        }

        // Check the item tag
        if (other.CompareTag(requiredItemTag))
        {
            if (enableDebugLog)
                Debug.Log($"[TaskCompleter] ✅ Correct task item detected: {other.name} (Tag: {requiredItemTag})");

            // Destroy the task item
            Destroy(other.gameObject);

            // Start the task completion sequence.
            StartTaskCompletionSequence();
        }
        else
        {
            if (enableDebugLog)
                Debug.Log($"[TaskCompleter] ❌ Item tag mismatch: Required '{requiredItemTag}', Actual '{other.tag}'");
        }
    }

    /// <summary>
    /// Initiates the task completion sequence (play particles, delayed destroy, and THEN notify manager).
    /// </summary>
    private void StartTaskCompletionSequence()
    {
        isTaskFinished = true; // Mark task as completed
        
        // Start the coroutine for particle playback and delayed destruction of THIS GameObject
        // The manager notification is moved into the coroutine to prevent premature destruction by the external TaskManager.
        StartCoroutine(TaskCompleteSequence(1.5f)); 
    }

    /// <summary>
    /// Coroutine for playing particle system and destroying THIS Task Completer GameObject after a delay.
    /// </summary>
    /// <param name="delay">The delay time before THIS GameObject is destroyed.</param>
    /// <returns>IEnumerator</returns>
    private IEnumerator TaskCompleteSequence(float delay)
    {
        // 1. Play particles immediately
        if (completionParticle != null)
        {
            completionParticle.Play();
            if (enableDebugLog)
                Debug.Log($"[TaskCompleter] 💫 Playing particle system: {completionParticle.name}");
        }
        
        // 2. Wait for the specified delay (1.5 seconds)
        yield return new WaitForSeconds(delay);
        
        // 3. Notify the Task Manager that the task is complete. 
        // We do this now, right before destruction, to allow time for the particle effect.
        if (taskManager != null)
        {
            // Note: We need the TaskData ID. Assuming TaskHandler stores TaskData, or we use the taskIndex.
            // Using TaskIndex as it's passed during initialization and used in TaskManager's callback.
            TaskData taskData = taskManager.GetDailyTasks().Find(t => taskManager.GetDailyTasks().IndexOf(t) == taskIndex);
            
            if (taskData != null)
            {
                taskManager.TaskCompleted(taskData.taskId, taskIndex);
            }
            else
            {
                 // Fallback if taskData lookup is difficult/impossible at this stage
                 // This ensures the task is completed even if the ID is missing.
                 Debug.LogWarning("[TaskCompleter] Could not find TaskData for completion notification, using TaskIndex.");
                 taskManager.TaskCompleted(-1, taskIndex); 
            }
        }

        // 4. Destroy THIS GameObject (Task Completer)
        if (enableDebugLog)
            Debug.Log($"[TaskCompleter] 🔥 Delayed destruction of Task Completer GameObject after {delay} seconds: {gameObject.name}");
        
        Destroy(gameObject);
    }

    /// <summary>
    /// Manually completes the task (for debugging).
    /// </summary>
    [ContextMenu("Manual Complete Task")]
    public void ManualCompleteTask()
    {
        if (!isInitialized || taskManager == null || isTaskFinished)
        {
            Debug.LogWarning("[TaskCompleter] Not ready, manager is null, or already finished, cannot manually complete task.");
            return;
        }

        // Start the task completion sequence
        StartTaskCompletionSequence();

        if (enableDebugLog)
            Debug.Log("[TaskCompleter] Manually completed task.");
    }

    /// <summary>
    /// Checks the status of the Task Completer (for debugging).
    /// </summary>
    [ContextMenu("Check Status")]
    public void CheckStatus()
    {
        Debug.Log($"[TaskCompleter] === Task Completer Status ===");
        Debug.Log($"Is Initialized: {isInitialized}");
        Debug.Log($"Is Task Finished: {isTaskFinished}");
        Debug.Log($"Task Index: {taskIndex}");
        Debug.Log($"Required Item Tag: {requiredItemTag}");
        Debug.Log($"TaskManager Reference: {(taskManager != null ? "Set" : "Not Set")}");
        Debug.Log($"ParticleSystem Reference: {(completionParticle != null ? "Set" : "Not Set")}");
        Debug.Log($"Trigger Collider: {(triggerCollider != null ? "Set" : "Not Set")}");
        Debug.Log($"Task Description Text Component: {(taskDescriptionText != null ? "Set" : "Not Set")}");

        if (triggerCollider != null)
        {
            Debug.Log($"Collider Type: {triggerCollider.GetType().Name}");
            Debug.Log($"Is Trigger: {triggerCollider.isTrigger}");
        }

        if (taskDescriptionText != null)
        {
            Debug.Log($"Current Display Text: {taskDescriptionText.text}");
        }
    }

    /// <summary>
    /// Tests setting the text (for debugging).
    /// </summary>
    [ContextMenu("Test Set Text")]
    public void TestSetText()
    {
        SetTaskDescriptionText("Need Manual");
    }

    /// <summary>
    /// Draws the trigger area in the Scene view.
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
                // Simple representation for capsule
                Gizmos.DrawWireCube(capsule.center, new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2));
            }
        }
    }

    void OnDestroy()
    {
        if (enableDebugLog)
            Debug.Log($"[TaskCompleter] Task Completer destroyed - Task Index: {taskIndex}");
    }
}