using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Attached to an XR Interactor (e.g., XR Direct Interactor).
/// Listens for the controller's grab and release events, checking if the grabbed object has the ContrabandItem script.
/// </summary>
[RequireComponent(typeof(XRBaseInteractor))]
public class ContrabandGrabDetector : MonoBehaviour
{
    // Reference to the CharacterStatus script
    private CharacterStatus characterStatus;

    // Reference to the current Interactor
    private XRBaseInteractor interactor;

    void Start()
    {
        // 1. Find CharacterStatus in the scene
        // Assumes the CharacterStatus script is attached to an easily findable GameObject (like the player or Game Manager)
        characterStatus = FindObjectOfType<CharacterStatus>();
        if (characterStatus == null)
        {
            Debug.LogError("[ContrabandGrabDetector] Error: CharacterStatus component not found in the scene! Cannot update slacking status.");
            enabled = false;
            return;
        }
        else
        {
            Debug.Log($"[ContrabandGrabDetector] Successfully found CharacterStatus component on object: {characterStatus.gameObject.name}");
        }

        // 2. Get Interactor and subscribe to events
        interactor = GetComponent<XRBaseInteractor>();
        if (interactor != null)
        {
            Debug.Log($"[ContrabandGrabDetector] Successfully found XRBaseInteractor ({interactor.GetType().Name}), subscribing to events...");
            // Subscribe to the grab start event
            interactor.selectEntered.AddListener(OnGrabbed);
            // Subscribe to the grab release event
            interactor.selectExited.AddListener(OnReleased);
        }
        else
        {
            Debug.LogError("[ContrabandGrabDetector] Error: XRBaseInteractor component not found on this GameObject!");
            enabled = false;
        }
    }

    /// <summary>
    /// Called when the player grabs an item
    /// </summary>
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        string grabbedObjectName = args.interactableObject.transform.name;
        Debug.Log($"[ContrabandGrabDetector] ✋ Grab Event Received! Grabbed object: {grabbedObjectName}");

        // Check if the grabbed object (args.interactableObject) has the ContrabandItem component
        ContrabandItem contraband = args.interactableObject.transform.GetComponent<ContrabandItem>();

        if (contraband != null)
        {
            // If it has ContrabandItem, it means it's contraband
            if (characterStatus != null)
            {
                characterStatus.isSlackingAtWork = true;
                Debug.Log($"[ContrabandGrabDetector] ✅ Identified as Contraband ({grabbedObjectName})! Slacking status (isSlackingAtWork) set to: True");
            }
            else
            {
                Debug.LogError("[ContrabandGrabDetector] ❌ Contraband found, but characterStatus reference is lost, cannot update status!");
            }
        }
        else
        {
            Debug.Log("[ContrabandGrabDetector] ❓ Grabbed object is not contraband. ContrabandItem script not found.");
        }
    }

    /// <summary>
    /// Called when the player releases an item
    /// </summary>
    private void OnReleased(SelectExitEventArgs args)
    {
        string releasedObjectName = args.interactableObject.transform.name;
        Debug.Log($"[ContrabandGrabDetector] 🗑️ Release Event Received! Released object: {releasedObjectName}");

        // Check if the released object (args.interactableObject) has the ContrabandItem component
        ContrabandItem contraband = args.interactableObject.transform.GetComponent<ContrabandItem>();

        if (contraband != null)
        {
            // Only reset the slacking status to false if contraband was released
            if (characterStatus != null)
            {
                characterStatus.isSlackingAtWork = false;
                Debug.Log($"[ContrabandGrabDetector] ✅ Contraband released ({releasedObjectName}). Slacking status (isSlackingAtWork) set to: False");
            }
        }
        else
        {
            // If released object is not contraband, do nothing or log for debug
            // The status should have been set to false by other means if necessary, or kept true if other contraband is held.
            // For simplicity, we only reset the status when contraband is explicitly released.
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks or calling on destroyed objects
        if (interactor != null)
        {
            interactor.selectEntered.RemoveListener(OnGrabbed);
            interactor.selectExited.RemoveListener(OnReleased);
            Debug.Log("[ContrabandGrabDetector] Events unsubscribed.");
        }
    }
}