using UnityEngine;

public class CharacterStatus : MonoBehaviour
{
    [Header("Status Settings")]
    public bool isSlackingAtWork;

    [Header("Penalty Settings")]
    public int penaltyAmount = 50; // Amount of salary deducted each time
    public float penaltyCooldown = 3f; // Penalty cooldown time (seconds)
    public float stressPenalty = 20f; // Stress value added per penalty

    [Header("Audio Settings")]
    [SerializeField] private AudioSource penaltyAudioSource; // Penalty sound player
    [SerializeField] private AudioClip penaltySound; // Penalty sound file
    [SerializeField, Range(0f, 1f)] private float penaltyVolume = 1f; // Penalty sound volume

    [Header("Debug Settings")]
    public bool enablePenaltyDebug = true; // Enable penalty debug information

    // Private variables
    private float lastPenaltyTime = -999f; // Time of the last penalty
    private GameLogicSystem gameLogicSystem; // Reference to the Game Logic System

    /// <summary>
    /// Manually test the penalty sound (for debugging)
    /// </summary>
    [ContextMenu("Test Penalty Sound")]
    public void TestPenaltySound()
    {
        PlayPenaltySound();
    }

    // Property accessor: get current stress level (from GameLogicSystem)
    public float stressLevel => gameLogicSystem != null ? gameLogicSystem.StressLevel : 0f;

    void Start()
    {
        // Get the GameLogicSystem component
        gameLogicSystem = FindObjectOfType<GameLogicSystem>();
        if (gameLogicSystem == null)
        {
            Debug.LogError("[CharacterStatus] GameLogicSystem not found in the scene.");
        }
    }

    /// <summary>
    /// Apply a penalty (e.g., salary deduction, stress increase) if the cooldown allows.
    /// </summary>
    /// <returns>True if penalty was applied, false otherwise.</returns>
    public bool ApplyPenalty()
    {
        if (Time.time >= lastPenaltyTime + penaltyCooldown)
        {
            // 1. Apply Salary Deduction
            gameLogicSystem?.DeductSalary(penaltyAmount);

            // 2. Increase Stress
            gameLogicSystem?.AddStress(stressPenalty);

            // 3. Play Sound Effect
            PlayPenaltySound();

            // 4. Update Cooldown
            lastPenaltyTime = Time.time;

            if (enablePenaltyDebug)
            {
                Debug.Log($"[CharacterStatus] ⚠️ Penalty applied! Deducted: ${penaltyAmount}, Stress +{stressPenalty}. Next penalty available in {penaltyCooldown:F1}s.");
            }

            return true;
        }
        else
        {
            if (enablePenaltyDebug)
            {
                float timeRemaining = (lastPenaltyTime + penaltyCooldown) - Time.time;
                Debug.Log($"[CharacterStatus] Penalty on cooldown. Remaining time: {timeRemaining:F1}s.");
            }
            return false;
        }
    }

    /// <summary>
    /// Play the penalty sound effect.
    /// </summary>
    private void PlayPenaltySound()
    {
        // Way 1: Use AudioSource.PlayOneShot (recommended for non-looping sound effects)
        if (penaltyAudioSource != null && penaltySound != null)
        {
            penaltyAudioSource.PlayOneShot(penaltySound, penaltyVolume);

            if (enablePenaltyDebug)
            {
                Debug.Log("[CharacterStatus] 🔊 Playing penalty sound (PlayOneShot)");
            }
        }
        // Way 2: Use AudioSource direct playback (if clip is already set and not using PlayOneShot)
        else if (penaltyAudioSource != null)
        {
            penaltyAudioSource.volume = penaltyVolume;
            penaltyAudioSource.Play();

            if (enablePenaltyDebug)
            {
                Debug.Log("[CharacterStatus] 🔊 Playing penalty sound (using preset clip)");
            }
        }
        // Way 3: Use AudioSource.PlayClipAtPoint (3D sound)
        else if (penaltySound != null)
        {
            AudioSource.PlayClipAtPoint(penaltySound, transform.position, penaltyVolume);

            if (enablePenaltyDebug)
            {
                Debug.Log("[CharacterStatus] 🔊 Playing penalty sound (3D positional sound)");
            }
        }
        else if (enablePenaltyDebug)
        {
            Debug.LogWarning("[CharacterStatus] ⚠️ Cannot play penalty sound: AudioSource or AudioClip not set.");
        }
    }

    /// <summary>
    /// Resets the penalty cooldown (for debugging or special cases).
    /// </summary>
    [ContextMenu("Reset Penalty Cooldown")]
    public void ResetPenaltyCooldown()
    {
        lastPenaltyTime = -999f;
        if (enablePenaltyDebug)
        {
            Debug.Log("[CharacterStatus] Penalty cooldown has been reset.");
        }
    }
}