using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Generic 3D VR Button Controller (suitable for Cube/Mesh)
/// Handles XR Interaction Toolkit Select/Hover events and provides visual, audio and haptic feedback.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class VR3DButton : MonoBehaviour
{
    [Header("Core Functionality")]
    [Tooltip("Event invoked when the button is successfully selected (pressed)")]
    public UnityEvent OnClicked = new UnityEvent();

    [Header("XR Interaction Components")]
    [SerializeField] private XRSimpleInteractable buttonInteractable;
    [SerializeField] private Transform buttonTransform;

    [Header("Visual Feedback")]
    [SerializeField] private Vector3 pressedPositionOffset = new Vector3(0, -0.01f, 0);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color pressedColor = Color.green;
    [SerializeField] private Color disabledColor = Color.gray;
    [SerializeField] private Renderer buttonRenderer;

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField][Range(0f, 1f)] private float hoverVolume = 0.5f;

    [Header("Haptic Feedback")]
    [SerializeField] private float hapticIntensity = 0.5f;
    [SerializeField] private float hapticDuration = 0.1f;

    // Private variables
    private Vector3 originalPosition;
    private Material buttonMaterial;
    private bool isPressed = false;
    private bool canInteract = true; // State management inspired by VRPrinterButton

    void Start()
    {
        InitializeButton();
    }

    /// <summary>
    /// Initialize the button
    /// </summary>
    private void InitializeButton()
    {
        // Auto fetch components
        if (buttonInteractable == null)
            buttonInteractable = GetComponent<XRSimpleInteractable>();

        if (buttonTransform == null)
            buttonTransform = transform;

        if (buttonRenderer == null)
            buttonRenderer = GetComponent<Renderer>();

        // Record original position
        originalPosition = buttonTransform.localPosition;

        // Set button material and initial color
        if (buttonRenderer != null)
        {
            buttonMaterial = buttonRenderer.material;
            SetButtonColor(normalColor);
            Debug.Log($"[VR3DButton:{gameObject.name}] Material initialization attempted.");
        }
        else
        {
            Debug.LogError($"[VR3DButton:{gameObject.name}] Renderer component is null! Cannot get material.");
        }

        // Bind interaction events
        if (buttonInteractable != null)
        {
            buttonInteractable.selectEntered.AddListener(OnButtonPressed);
            buttonInteractable.selectExited.AddListener(OnButtonReleased);
            buttonInteractable.hoverEntered.AddListener(OnButtonHover);
            buttonInteractable.hoverExited.AddListener(OnButtonHoverExit);
            Debug.Log($"[VR3DButton:{gameObject.name}] Event listeners bound.");
        }
        else
        {
            Debug.LogError("[VR3DButton] Missing XRSimpleInteractable component!");
        }
    }

    /// <summary>
    /// Called when the button is pressed (ray click or direct touch)
    /// </summary>
    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        Debug.Log($"[VR3DButton:{gameObject.name}] OnButtonPressed attempted.");

        if (!canInteract)
        {
            Debug.LogWarning($"[VR3DButton:{gameObject.name}] Button is disabled (canInteract=false), blocking action.");
            return;
        }

        if (isPressed)
        {
            Debug.LogWarning($"[VR3DButton:{gameObject.name}] Button already in pressed state (isPressed=true), blocking action.");
            return;
        }

        isPressed = true;
        Debug.Log($"[VR3DButton:{gameObject.name}] Entered pressed state, executing feedback.");

        // Visual feedback: pressed position and color
        SetButtonPressed(true);
        SetButtonColor(pressedColor);

        // Haptic feedback
        SendHapticFeedback(args.interactorObject);
    }

    /// <summary>
    /// Called when the button is released
    /// </summary>
    private void OnButtonReleased(SelectExitEventArgs args)
    {
        Debug.Log($"[VR3DButton:{gameObject.name}] OnButtonReleased triggered.");

        if (!isPressed)
        {
            Debug.LogWarning($"[VR3DButton:{gameObject.name}] isPressed is false on release, ignoring.");
            return;
        }

        isPressed = false;

        // Invoke external click event
        OnClicked.Invoke();
        Debug.Log($"[VR3DButton:{gameObject.name}] OnClicked event invoked.");

        // Restore button state
        SetButtonPressed(false);
        SetButtonColor(normalColor);
    }

    /// <summary>
    /// Hover enter by ray or touch
    /// </summary>
    private void OnButtonHover(HoverEnterEventArgs args)
    {
        Debug.Log($"[VR3DButton:{gameObject.name}] OnButtonHover triggered.");

        if (!canInteract) return;

        // Hover color change
        if (!isPressed)
        {
            SetButtonColor(hoverColor);
        }

        // Play hover sound
        if (AudioManager.Instance != null && hoverSound != null)
        {
            AudioManager.Instance.PlaySFX(hoverSound, hoverVolume);
        }
    }

    /// <summary>
    /// Hover exit by ray or touch
    /// </summary>
    private void OnButtonHoverExit(HoverExitEventArgs args)
    {
        Debug.Log($"[VR3DButton:{gameObject.name}] OnButtonHoverExit triggered.");

        if (!canInteract) return;

        // Restore normal color
        if (!isPressed)
        {
            SetButtonColor(normalColor);
        }
    }

    /// <summary>
    /// Set button color (with material checks)
    /// </summary>
    private void SetButtonColor(Color color)
    {
        if (buttonMaterial != null)
        {
            Debug.Log($"[VR3DButton:{gameObject.name}] Attempting to set color: {color}");
            // Compatible with URP/HDRP or Standard Shader
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
            Debug.LogError($"[VR3DButton:{gameObject.name}] Material is null (buttonMaterial is null)! Cannot set color.");
        }
    }

    /// <summary>
    /// Set button interactable state (inspired by VRPrinterButton state management)
    /// </summary>
    public void SetInteractable(bool canInteract)
    {
        this.canInteract = canInteract;

        if (buttonInteractable != null)
        {
            buttonInteractable.enabled = canInteract;
        }

        // Update visual state
        Color targetColor = canInteract ? normalColor : disabledColor;
        SetButtonColor(targetColor);
        Debug.Log($"[VR3DButton:{gameObject.name}] Interactable state set to: {canInteract}");
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
        // Cleanup event listeners
        if (buttonInteractable != null)
        {
            buttonInteractable.selectEntered.RemoveListener(OnButtonPressed);
            buttonInteractable.selectExited.RemoveListener(OnButtonReleased);
            buttonInteractable.hoverEntered.RemoveListener(OnButtonHover);
            buttonInteractable.hoverExited.RemoveListener(OnButtonHoverExit);
        }
    }
}