using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class VRFoodConsumer : MonoBehaviour
{
    [Header("Laser Detection Settings")]
    public Transform headTransform; // Head Transform (usually the Main Camera)
    public float laserLength = 0.5f; // Laser length
    public float coneAngle = 30f; // Cone angle (degrees)
    public LayerMask foodLayerMask = -1; // Food layer mask

    [Header("Consumption Settings")]
    public float consumeDuration = 2f; // Duration to consume (seconds)
    public ParticleSystem eatingEffectPrefab; // Particle system prefab for eating effect
    public Transform eatingEffectSpawnPoint; // Spawn point for the particle effect (e.g. mouth position)

    [Header("UI Settings")]
    public Slider speedBoostSlider; // Speed boost countdown slider (assign your Slider here)

    [Header("Debug Settings")]
    public bool showDebugLaser = true; // Whether to show debug laser/cone

    // Private variables
    private CharacterController characterController;
    private GameLogicSystem gameLogicSystem;
    private CharacterStatus characterStatus; // Reference to character status component

    // Consumption state
    private bool isEating = false; // Whether currently eating
    private Coroutine eatingCoroutine;
    private ParticleSystem currentEatingEffect; // Currently instantiated particle system

    // Speed boost info
    private bool hasSpeedBoost = false;
    private float originalMoveSpeed = 2f; // Original move speed
    private float boostedMoveSpeed = 2f; // Boosted move speed
    private Coroutine speedBoostCoroutine;

    void Start()
    {
        // Get components
        characterController = GetComponent<CharacterController>();
        gameLogicSystem = FindObjectOfType<GameLogicSystem>();
        characterStatus = GetComponent<CharacterStatus>(); // Get character status component

        // If head transform not specified, try to find Main Camera
        if (headTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                headTransform = mainCamera.transform;
            }
        }

        // If effect spawn point not specified, use head transform
        if (eatingEffectSpawnPoint == null)
        {
            eatingEffectSpawnPoint = headTransform;
        }

        // Initialize speed boost UI state
        if (speedBoostSlider != null)
        {
            speedBoostSlider.gameObject.SetActive(false);
        }

        // Get original move speed from movement provider if available
        // Note: CharacterController itself doesn't have moveSpeed property
        // Adjust according to your movement implementation
        if (characterController != null)
        {
            var moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
            if (moveProvider != null)
            {
                originalMoveSpeed = moveProvider.moveSpeed;
            }
        }
    }

    void Update()
    {
        // Detect food with laser/cone and auto-consume
        DetectAndConsumeFood();
    }

    /// <summary>
    /// Detect food with a cone-shaped laser and auto-consume
    /// </summary>
    private void DetectAndConsumeFood()
    {
        if (headTransform == null || isEating) return;

        // Debug draw cone if enabled
        if (showDebugLaser)
        {
            DrawDebugCone();
        }

        // Cone detection
        FoodItem closestFood = DetectFoodInCone();

        if (closestFood != null)
        {
            // If laser hits food, start consuming
            if (eatingCoroutine != null)
            {
                StopCoroutine(eatingCoroutine);
            }
            eatingCoroutine = StartCoroutine(ConsumeFood(closestFood));
        }
    }

    /// <summary>
    /// Detect food within cone range
    /// </summary>
    /// <returns>Closest food item or null if none</returns>
    private FoodItem DetectFoodInCone()
    {
        Vector3 headPosition = headTransform.position;
        Vector3 headForward = headTransform.forward;

        // Use OverlapSphere to find colliders in range
        Collider[] colliders = Physics.OverlapSphere(headPosition, laserLength, foodLayerMask);

        FoodItem closestFood = null;
        float closestDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            // Direction to the food
            Vector3 directionToFood = (collider.transform.position - headPosition).normalized;

            // Angle between forward and direction to food
            float angle = Vector3.Angle(headForward, directionToFood);

            // Check if inside cone angle
            if (angle <= coneAngle * 0.5f)
            {
                // Check for FoodItem component
                FoodItem foodItem = collider.GetComponent<FoodItem>();
                if (foodItem != null)
                {
                    // Pick the closest food
                    float distance = Vector3.Distance(headPosition, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestFood = foodItem;
                    }
                }
            }
        }

        return closestFood;
    }

    /// <summary>
    /// Draw debug cone laser
    /// </summary>
    private void DrawDebugCone()
    {
        Vector3 headPosition = headTransform.position;
        Vector3 headForward = headTransform.forward;
        Vector3 headUp = headTransform.up;
        Vector3 headRight = headTransform.right;

        // Cone end radius
        float coneRadius = laserLength * Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad);

        // Center of cone end
        Vector3 coneEndCenter = headPosition + headForward * laserLength;

        // Center line
        Debug.DrawRay(headPosition, headForward * laserLength, Color.red);

        // Cone edge lines (8 segments)
        int segments = 8;
        for (int i = 0; i < segments; i++)
        {
            float angle = (360f / segments) * i * Mathf.Deg2Rad;
            Vector3 direction = headUp * Mathf.Sin(angle) + headRight * Mathf.Cos(angle);
            Vector3 coneEdgePoint = coneEndCenter + direction * coneRadius;

            // Line from head to cone edge
            Debug.DrawLine(headPosition, coneEdgePoint, Color.yellow);

            // Circle outline at cone end
            if (i < segments - 1)
            {
                float nextAngle = (360f / segments) * (i + 1) * Mathf.Deg2Rad;
                Vector3 nextDirection = headUp * Mathf.Sin(nextAngle) + headRight * Mathf.Cos(nextAngle);
                Vector3 nextConeEdgePoint = coneEndCenter + nextDirection * coneRadius;
                Debug.DrawLine(coneEdgePoint, nextConeEdgePoint, Color.green);
            }
            else
            {
                // Connect last segment to first
                Vector3 firstDirection = headUp * Mathf.Sin(0) + headRight * Mathf.Cos(0);
                Vector3 firstConeEdgePoint = coneEndCenter + firstDirection * coneRadius;
                Debug.DrawLine(coneEdgePoint, firstConeEdgePoint, Color.green);
            }
        }
    }

    /// <summary>
    /// Instantiate eating particle effect
    /// </summary>
    private void StartEatingEffect()
    {
        if (eatingEffectPrefab != null && eatingEffectSpawnPoint != null)
        {
            // Instantiate particle system at spawn point
            currentEatingEffect = Instantiate(eatingEffectPrefab, eatingEffectSpawnPoint.position, eatingEffectSpawnPoint.rotation);

            // Parent to spawn point so it follows
            currentEatingEffect.transform.SetParent(eatingEffectSpawnPoint);

            // Play particle system
            currentEatingEffect.Play();

            Debug.Log("Started eating particle effect");
        }
        else
        {
            Debug.LogWarning("Eating effect prefab or spawn point not set");
        }
    }

    /// <summary>
    /// Apply particle colors to the current eating effect
    /// </summary>
    /// <param name="startColor">Start color</param>
    /// <param name="endColor">End color</param>
    private void ApplyParticleColors(Color startColor, Color endColor)
    {
        if (currentEatingEffect != null)
        {
            // Access main module
            var main = currentEatingEffect.main;

            // Option 1: use two-color MinMaxGradient (recommended)
            ParticleSystem.MinMaxGradient gradient = new ParticleSystem.MinMaxGradient(startColor, endColor);

            // Set gradient mode to TwoColors
            gradient.mode = ParticleSystemGradientMode.TwoColors;

            main.startColor = gradient;

            Debug.Log($"Particle colors set to Start: {startColor}, End: {endColor}");
        }
    }

    /// <summary>
    /// Stop and destroy the eating particle effect
    /// </summary>
    private void StopEatingEffect()
    {
        if (currentEatingEffect != null)
        {
            // Stop particle system
            currentEatingEffect.Stop();

            // Wait until stopped then destroy
            StartCoroutine(DestroyEffectAfterStop(currentEatingEffect));

            currentEatingEffect = null;

            Debug.Log("Stopped eating particle effect");
        }
    }

    /// <summary>
    /// Wait for particle system to stop before destroying
    /// </summary>
    private IEnumerator DestroyEffectAfterStop(ParticleSystem effect)
    {
        // Wait until particle system is no longer playing
        while (effect != null && effect.isPlaying)
        {
            yield return null;
        }

        // Destroy the particle system GameObject
        if (effect != null)
        {
            Destroy(effect.gameObject);
        }
    }

    /// <summary>
    /// Consume food coroutine (with delay)
    /// </summary>
    /// <param name="foodItem">Food item to consume</param>
    private IEnumerator ConsumeFood(FoodItem foodItem)
    {
        isEating = true;

        // Set slacking state true
        if (characterStatus != null)
        {
            characterStatus.isSlackingAtWork = true;
        }

        // Get food effect
        FoodEffect effect = foodItem.GetFoodEffect();

        // Start eating particle effect
        StartEatingEffect();

        // Apply food particle colors to effect
        ApplyParticleColors(effect.particleStartColor, effect.particleEndColor);

        Debug.Log($"Started consuming: {foodItem.foodName}");

        // If effect reduces stress, apply gradually during consumption
        float stressReductionPerSecond = 0f;
        if (effect.hasStressReduction)
        {
            stressReductionPerSecond = effect.stressReduction / consumeDuration;
        }

        // Eating process
        float elapsedTime = 0f;
        while (elapsedTime < consumeDuration)
        {
            elapsedTime += Time.deltaTime;

            // Gradually reduce stress
            if (effect.hasStressReduction && gameLogicSystem != null)
            {
                float stressToReduce = stressReductionPerSecond * Time.deltaTime;
                gameLogicSystem.ReduceStress(stressToReduce);
            }

            yield return null;
        }

        // Apply speed boost immediately after eating if present
        if (effect.hasSpeedBoost)
        {
            ApplySpeedBoost(effect.speedMultiplier, effect.speedBoostDuration);
            Debug.Log($"Gained speed boost: {effect.speedMultiplier}x for {effect.speedBoostDuration} seconds");
        }

        // Stop eating effect
        StopEatingEffect();

        // Consume the food (destroy or notify)
        foodItem.OnConsume();

        Debug.Log($"Finished consuming: {foodItem.foodName}");

        isEating = false;

        // Set slacking state false
        if (characterStatus != null)
        {
            characterStatus.isSlackingAtWork = false;
        }

        eatingCoroutine = null;
    }

    /// <summary>
    /// Apply food effect (currently only speed boost handled; stress reduction handled in coroutine)
    /// </summary>
    /// <param name="effect">Food effect</param>
    private void ApplyFoodEffect(FoodEffect effect)
    {
        // Speed boost handled at end of coroutine
        // Stress reduction handled gradually during coroutine
    }

    /// <summary>
    /// Apply speed boost
    /// </summary>
    /// <param name="multiplier">Speed multiplier</param>
    /// <param name="duration">Duration in seconds</param>
    private void ApplySpeedBoost(float multiplier, float duration)
    {
        // If already boosted, stop previous coroutine
        if (speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
        }

        speedBoostCoroutine = StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
    }

    /// <summary>
    /// Speed boost coroutine
    /// </summary>
    private IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        hasSpeedBoost = true;
        boostedMoveSpeed = originalMoveSpeed * multiplier;

        // Show speed boost UI
        if (speedBoostSlider != null)
        {
            speedBoostSlider.gameObject.SetActive(true);
            speedBoostSlider.maxValue = duration;
            speedBoostSlider.value = duration;
        }

        // Apply boosted speed to movement provider (adjust to your movement implementation)
        var moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = boostedMoveSpeed;
        }

        // Countdown update for slider
        float remainingTime = duration;
        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            remainingTime = Mathf.Max(0, remainingTime);

            // Update slider
            if (speedBoostSlider != null)
            {
                speedBoostSlider.value = remainingTime;
            }

            yield return null;
        }

        // Restore original speed
        hasSpeedBoost = false;
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = originalMoveSpeed;
        }

        // Hide speed boost UI
        if (speedBoostSlider != null)
        {
            speedBoostSlider.gameObject.SetActive(false);
        }

        speedBoostCoroutine = null;
        Debug.Log("Speed boost effect ended");
    }

    /// <summary>
    /// Manually trigger consume (if needed)
    /// Mainly auto-triggered by laser; this is a fallback
    /// </summary>
    public void ManualConsumeFood()
    {
        if (headTransform == null || isEating) return;

        // Use cone detection
        FoodItem closestFood = DetectFoodInCone();

        if (closestFood != null)
        {
            if (eatingCoroutine != null)
            {
                StopCoroutine(eatingCoroutine);
            }
            eatingCoroutine = StartCoroutine(ConsumeFood(closestFood));
        }
    }

    /// <summary>
    /// Stop eating (for interruptions)
    /// </summary>
    public void StopEating()
    {
        if (isEating && eatingCoroutine != null)
        {
            StopCoroutine(eatingCoroutine);

            // Stop particle effect
            StopEatingEffect();

            isEating = false;

            // Set slacking state false
            if (characterStatus != null)
            {
                characterStatus.isSlackingAtWork = false;
            }

            eatingCoroutine = null;
            Debug.Log("Stopped consuming");
        }
    }

    /// <summary>
    /// Stop speed boost (for interruptions)
    /// </summary>
    public void StopSpeedBoost()
    {
        if (hasSpeedBoost && speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);

            // Restore original speed
            hasSpeedBoost = false;
            var moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
            if (moveProvider != null)
            {
                moveProvider.moveSpeed = originalMoveSpeed;
            }

            // Hide speed boost UI
            if (speedBoostSlider != null)
            {
                speedBoostSlider.gameObject.SetActive(false);
            }

            speedBoostCoroutine = null;
            Debug.Log("Speed boost effect interrupted");
        }
    }

    // Property accessors
    public bool IsEating => isEating;
    public bool HasSpeedBoost => hasSpeedBoost;
    public float CurrentMoveSpeed => hasSpeedBoost ? boostedMoveSpeed : originalMoveSpeed;
    public float SpeedBoostTimeRemaining => speedBoostSlider != null ? speedBoostSlider.value : 0f;

    // Debug gizmos
    void OnDrawGizmosSelected()
    {
        if (headTransform != null)
        {
            Vector3 headPosition = headTransform.position;
            Vector3 headForward = headTransform.forward;
            Vector3 headUp = headTransform.up;
            Vector3 headRight = headTransform.right;

            // Cone end radius
            float coneRadius = laserLength * Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad);
            Vector3 coneEndCenter = headPosition + headForward * laserLength;

            // Draw cone outline
            Gizmos.color = Color.red;

            // Center line
            Gizmos.DrawRay(headPosition, headForward * laserLength);

            // Cone edge lines
            int segments = 12;
            Vector3[] conePoints = new Vector3[segments];

            for (int i = 0; i < segments; i++)
            {
                float angle = (360f / segments) * i * Mathf.Deg2Rad;
                Vector3 direction = headUp * Mathf.Sin(angle) + headRight * Mathf.Cos(angle);
                Vector3 coneEdgePoint = coneEndCenter + direction * coneRadius;
                conePoints[i] = coneEdgePoint;

                // Line from head to cone edge
                Gizmos.DrawLine(headPosition, coneEdgePoint);
            }

            // Draw circle at cone end
            Gizmos.color = Color.yellow;
            for (int i = 0; i < segments; i++)
            {
                int nextIndex = (i + 1) % segments;
                Gizmos.DrawLine(conePoints[i], conePoints[nextIndex]);
            }

            // Draw center point of cone end
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(coneEndCenter, 0.05f);
        }
    }

    void OnDestroy()
    {
        // Ensure particle effect cleaned up on destroy
        if (currentEatingEffect != null)
        {
            Destroy(currentEatingEffect.gameObject);
        }
    }
}