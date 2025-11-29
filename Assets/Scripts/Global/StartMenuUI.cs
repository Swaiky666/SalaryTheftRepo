using UnityEngine;
using UnityEngine.UI;
using TMPro; // 如果使用TextMeshPro

/// <summary>
/// 开始菜单UI控制器
/// 管理主菜单按钮和设置面板
/// </summary>
public class StartMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Settings UI")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button backButton;

    [Header("Optional: Volume Text Display")]
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;

    [Header("References")]
    [SerializeField] private SceneController sceneController;

    [Header("Audio Feedback (Optional)")]
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip buttonHoverSFX;

    private void Start()
    {
        // 确保SceneController存在
        if (sceneController == null)
        {
            sceneController = FindObjectOfType<SceneController>();
            if (sceneController == null)
            {
                GameObject sceneControllerObj = new GameObject("SceneController");
                sceneController = sceneControllerObj.AddComponent<SceneController>();
            }
        }

        // 初始化UI
        InitializeUI();
        
        // 绑定按钮事件
        BindButtonEvents();
        
        // 初始化音量滑条
        InitializeVolumeSliders();

        // 默认显示主菜单
        ShowMainMenu();
    }

    /// <summary>
    /// 初始化UI组件
    /// </summary>
    private void InitializeUI()
    {
        // 确保面板存在
        if (mainMenuPanel == null)
            Debug.LogWarning("Main Menu Panel未分配！");
        if (settingsPanel == null)
            Debug.LogWarning("Settings Panel未分配！");
    }

    /// <summary>
    /// 绑定按钮点击事件
    /// </summary>
    private void BindButtonEvents()
    {
        // 主菜单按钮
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        // 设置面板返回按钮
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        // 音量滑条事件
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    /// <summary>
    /// 初始化音量滑条
    /// </summary>
    private void InitializeVolumeSliders()
    {
        if (AudioManager.Instance != null)
        {
            // 设置滑条初始值
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

    #region Button Click Handlers

    /// <summary>
    /// 开始游戏按钮点击
    /// </summary>
    private void OnStartGameClicked()
    {
        PlayButtonClickSFX();
        Debug.Log("开始游戏 - 加载InGame场景");
        sceneController.LoadInGameScene();
    }

    /// <summary>
    /// 设置按钮点击
    /// </summary>
    private void OnSettingsClicked()
    {
        PlayButtonClickSFX();
        Debug.Log("打开游戏设置");
        ShowSettingsPanel();
    }

    /// <summary>
    /// 退出游戏按钮点击
    /// </summary>
    private void OnQuitClicked()
    {
        PlayButtonClickSFX();
        Debug.Log("退出游戏");
        sceneController.QuitGame();
    }

    /// <summary>
    /// 返回按钮点击
    /// </summary>
    private void OnBackClicked()
    {
        PlayButtonClickSFX();
        Debug.Log("返回主菜单");
        ShowMainMenu();
    }

    #endregion

    #region Volume Control

    /// <summary>
    /// 音乐音量改变
    /// </summary>
    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
            UpdateMusicVolumeText(value);
        }
    }

    /// <summary>
    /// 音效音量改变
    /// </summary>
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

    /// <summary>
    /// 更新音乐音量文本显示
    /// </summary>
    private void UpdateMusicVolumeText(float value)
    {
        if (musicVolumeText != null)
            musicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    /// <summary>
    /// 更新音效音量文本显示
    /// </summary>
    private void UpdateSFXVolumeText(float value)
    {
        if (sfxVolumeText != null)
            sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    #endregion

    #region Panel Management

    /// <summary>
    /// 显示主菜单
    /// </summary>
    private void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    /// <summary>
    /// 显示设置面板
    /// </summary>
    private void ShowSettingsPanel()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    #endregion

    #region Audio Feedback

    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    private void PlayButtonClickSFX()
    {
        if (AudioManager.Instance != null && buttonClickSFX != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSFX);
        }
    }

    /// <summary>
    /// 播放按钮悬停音效（可选）
    /// </summary>
    public void PlayButtonHoverSFX()
    {
        if (AudioManager.Instance != null && buttonHoverSFX != null)
        {
            AudioManager.Instance.PlaySFX(buttonHoverSFX, 0.5f);
        }
    }

    #endregion
}