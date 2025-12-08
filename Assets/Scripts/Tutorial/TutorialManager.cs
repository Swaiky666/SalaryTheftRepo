using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// VR 教程管理器
/// 控制教程的步骤、玩家位置移动和 UI 文本显示
/// </summary>
public class TutorialManager : MonoBehaviour
{
    private const string TutorialCompleteKey = "TutorialCompleted";

    [Header("References")]
    [SerializeField] private VR3DButton nextStepButton;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private SceneController sceneController;

    [Header("Tutorial Steps")]
    [Tooltip("教程步骤列表，每个步骤包含目标位置和要显示的文本")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    private int currentStepIndex = 0;

    // **新增：当前步骤的按钮目标 Transform**
    private Transform currentButtonTarget;

    [System.Serializable]
    private class TutorialStep
    {
        public string tutorialText;
        [Tooltip("玩家需要移动到的位置和旋转目标")]
        public Transform playerTargetTransform;

        [Tooltip("确认按钮要移动到的位置和旋转目标")]
        public Transform buttonTargetTransform;
    }

    void Start()
    {
        if (nextStepButton == null || tutorialText == null || playerTransform == null || sceneController == null)
        {
            Debug.LogError("TutorialManager: 缺少必要的引用！");
            return;
        }

        nextStepButton.OnClicked.AddListener(OnNextStepClicked);
        currentStepIndex = 0;
        StartTutorial();
    }

    // **【关键修复】在每一帧同步按钮的位置和旋转**
    void Update()
    {
        // 只有在教程进行中且当前步骤有按钮目标时才同步
        if (currentButtonTarget != null && nextStepButton != null)
        {
            nextStepButton.transform.position = currentButtonTarget.position;
            nextStepButton.transform.rotation = currentButtonTarget.rotation;
        }
    }

    private void StartTutorial()
    {
        if (steps.Count == 0)
        {
            Debug.LogWarning("教程步骤列表为空，直接进入下一场景。");
            GoToNextScene();
            return;
        }

        if (nextStepButton.gameObject.activeSelf == false)
            nextStepButton.gameObject.SetActive(true);

        ShowCurrentStep();
    }

    /// <summary>
    /// 显示当前步骤的文本，并将玩家和按钮移动到目标位置
    /// </summary>
    private void ShowCurrentStep()
    {
        if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            TutorialStep currentStep = steps[currentStepIndex];

            // 1. **移动玩家位置和旋转**
            if (currentStep.playerTargetTransform != null)
            {
                playerTransform.position = currentStep.playerTargetTransform.position;
                playerTransform.rotation = currentStep.playerTargetTransform.rotation;
            }

            // 2. **设置按钮同步目标 (位置将在 Update 中持续同步)**
            if (currentStep.buttonTargetTransform != null)
            {
                // **【关键修复】将目标存储到字段中**
                currentButtonTarget = currentStep.buttonTargetTransform;

                // 确保按钮是世界对象，避免被 Hierarchy 干扰
                nextStepButton.transform.SetParent(null);

                Debug.Log($"设置按钮同步目标到步骤 {currentStepIndex + 1} 的 Transform。");
            }
            else
            {
                // 如果没有目标，则停止同步
                currentButtonTarget = null;
                Debug.LogWarning($"步骤 {currentStepIndex + 1} 缺少按钮目标位置引用，按钮将保持原位且不再同步。");
            }

            // 3. **弹出文本教程**
            tutorialText.text = currentStep.tutorialText;

            // 确保按钮是可交互状态
            nextStepButton.SetInteractable(true);
        }
    }

    /// <summary>
    /// 当用户点击确认按钮时调用
    /// </summary>
    public void OnNextStepClicked()
    {
        // **在切换步骤时，立即停止对旧目标的同步**
        currentButtonTarget = null;

        currentStepIndex++;

        if (currentStepIndex < steps.Count)
        {
            ShowCurrentStep();
        }
        else
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        PlayerPrefs.SetInt(TutorialCompleteKey, 1);
        PlayerPrefs.Save();

        // 教程完成时，停止同步
        currentButtonTarget = null;

        nextStepButton.SetInteractable(false);
        nextStepButton.gameObject.SetActive(false);
        tutorialText.text = "教程结束！正在加载游戏...";

        GoToNextScene();
    }

    private void GoToNextScene()
    {
        sceneController.LoadInGameScene();
    }

    public static bool IsTutorialCompleted()
    {
        return PlayerPrefs.GetInt(TutorialCompleteKey, 0) == 1;
    }

    [ContextMenu("Reset Tutorial Status (For Testing)")]
    public static void ResetTutorialStatus()
    {
        PlayerPrefs.DeleteKey(TutorialCompleteKey);
        PlayerPrefs.Save();
        Debug.Log("教程完成状态已重置！");
    }
}