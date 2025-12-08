using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 开始菜单UI控制器
/// 管理主菜单按钮和设置面板，以及所有3D VR按钮的逻辑绑定
/// </summary>
public class StartMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Menu Buttons (VR3DButton 引用)")]
    [SerializeField] private VR3DButton startGameButton;
    [SerializeField] private VR3DButton settingsButton;
    [SerializeField] private VR3DButton quitButton;

    [Header("Settings UI")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private VR3DButton backButton;

    [Header("Volume Control Buttons")]
    [SerializeField] private VR3DButton musicVolumeUpButton;
    [SerializeField] private VR3DButton musicVolumeDownButton;
    [SerializeField] private VR3DButton sfxVolumeUpButton;
    [SerializeField] private VR3DButton sfxVolumeDownButton;

    [Header("Optional: Volume Text Display")]
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;

    [Header("References")]
    [SerializeField] private SceneController sceneController;

    // ********** 新增配置 **********
    [Header("新游戏设置")]
    [Tooltip("勾选此项则'开始游戏'按钮会加载'Tutorial'场景，否则加载'InGame'场景。")]
    [SerializeField] private bool loadTutorialScene = false;
    // ****************************

    [Header("Audio Feedback (Optional)")]
    [SerializeField] private AudioClip buttonClickSFX;

    private void Start()
    {
        InitializeUI();
        BindButtonEvents();
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        UnbindButtonEvents();
    }

    private void InitializeUI()
    {
        // 初始化音量滑块和文本
        if (AudioManager.Instance != null)
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
                UpdateMusicVolumeText(musicVolumeSlider.value);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
                UpdateSFXVolumeText(sfxVolumeSlider.value);
            }
        }
    }

    private void BindButtonEvents()
    {
        // 绑定主菜单按钮
        if (startGameButton != null)
            startGameButton.OnClicked.AddListener(OnStartGameClicked);
        if (settingsButton != null)
            settingsButton.OnClicked.AddListener(ShowSettingsPanel);
        if (quitButton != null)
            quitButton.OnClicked.AddListener(OnQuitGameClicked);

        // 绑定设置面板按钮
        if (backButton != null)
            backButton.OnClicked.AddListener(ShowMainMenu);

        // 绑定音量滑块事件
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // 绑定音量控制按钮 (可选)
        if (musicVolumeUpButton != null)
            musicVolumeUpButton.OnClicked.AddListener(() => ChangeVolume(musicVolumeSlider, 0.05f));
        if (musicVolumeDownButton != null)
            musicVolumeDownButton.OnClicked.AddListener(() => ChangeVolume(musicVolumeSlider, -0.05f));
        if (sfxVolumeUpButton != null)
            sfxVolumeUpButton.OnClicked.AddListener(() => ChangeVolume(sfxVolumeSlider, 0.05f));
        if (sfxVolumeDownButton != null)
            sfxVolumeDownButton.OnClicked.AddListener(() => ChangeVolume(sfxVolumeSlider, -0.05f));
    }

    private void UnbindButtonEvents()
    {
        // 解绑主菜单按钮
        if (startGameButton != null)
            startGameButton.OnClicked.RemoveListener(OnStartGameClicked);
        if (settingsButton != null)
            settingsButton.OnClicked.RemoveListener(ShowSettingsPanel);
        if (quitButton != null)
            quitButton.OnClicked.RemoveListener(OnQuitGameClicked);

        // 解绑设置面板按钮
        if (backButton != null)
            backButton.OnClicked.RemoveListener(ShowMainMenu);

        // 解绑音量滑块事件
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

        // 解绑音量控制按钮 (可选)
        if (musicVolumeUpButton != null)
            musicVolumeUpButton.OnClicked.RemoveAllListeners();
        if (musicVolumeDownButton != null)
            musicVolumeDownButton.OnClicked.RemoveAllListeners();
        if (sfxVolumeUpButton != null)
            sfxVolumeUpButton.OnClicked.RemoveAllListeners();
        if (sfxVolumeDownButton != null)
            sfxVolumeDownButton.OnClicked.RemoveAllListeners();
    }

    #region Button Handlers

    /// <summary>
    /// **处理开始游戏点击事件，根据 loadTutorialScene 决定加载场景**
    /// </summary>
    private void OnStartGameClicked()
    {
        PlayButtonClickSound();

        if (sceneController == null)
        {
            Debug.LogError("[StartMenuUI] SceneController 引用丢失，无法加载场景！请确保已在 Inspector 中设置。");
            return;
        }

        if (loadTutorialScene)
        {
            // 调用 SceneController 的方法，该方法会自动调用 GameLogicSystem.ResetGameProgress()
            sceneController.LoadTutorialScene();
        }
        else
        {
            // 调用 SceneController 的方法，该方法会自动调用 GameLogicSystem.ResetGameProgress()
            sceneController.LoadInGameScene();
        }
    }

    private void OnQuitGameClicked()
    {
        PlayButtonClickSound();
        if (sceneController != null)
        {
            sceneController.QuitGame();
        }
        else
        {
            Application.Quit();
        }
    }

    private void PlayButtonClickSound()
    {
        if (AudioManager.Instance != null && buttonClickSFX != null)
        {
            // 使用音效源播放音效，不随物体销毁而消失
            AudioManager.Instance.PlaySFX(buttonClickSFX);
        }
    }

    #endregion

    #region Volume Control

    private void ChangeVolume(Slider slider, float delta)
    {
        slider.value = Mathf.Clamp01(slider.value + delta);
        // 滑块的 onValueChanged 事件会自动触发 OnMusicVolumeChanged / OnSFXVolumeChanged
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
            UpdateMusicVolumeText(value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
            UpdateSFXVolumeText(value);

            // 播放测试音效
            if (buttonClickSFX != null)
                AudioManager.Instance.PlaySFX(buttonClickSFX);
        }
    }

    private void UpdateMusicVolumeText(float value)
    {
        if (musicVolumeText != null)
            musicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    private void UpdateSFXVolumeText(float value)
    {
        if (sfxVolumeText != null)
            sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    #endregion

    #region Panel Management

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ShowSettingsPanel()
    {
        PlayButtonClickSound();
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    #endregion
}