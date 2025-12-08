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

    [Header("Audio Feedback (Optional)")]
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip buttonErrorSFX; // 失败反馈音效

    private const float VolumeStep = 0.1f; // 音量调节步长

    private void Start()
    {
        if (sceneController == null)
        {
            sceneController = FindObjectOfType<SceneController>();
            if (sceneController == null)
            {
                GameObject sceneControllerObj = new GameObject("SceneController");
                sceneController = sceneControllerObj.AddComponent<SceneController>();
            }
        }

        BindButtonEvents();
        InitializeVolumeSliders();
        ShowMainMenu();
    }

    /// <summary>
    /// 绑定所有按钮和滑条的事件
    /// </summary>
    private void BindButtonEvents()
    {
        // 绑定主菜单按钮
        if (startGameButton != null)
            startGameButton.OnClicked.AddListener(OnStartGameClicked);

        if (settingsButton != null)
            settingsButton.OnClicked.AddListener(OnSettingsClicked);

        if (quitButton != null)
            quitButton.OnClicked.AddListener(OnQuitClicked);

        // 绑定设置面板返回按钮
        if (backButton != null)
            backButton.OnClicked.AddListener(OnBackClicked);

        // 绑定四个音量控制按钮事件
        if (musicVolumeUpButton != null)
            musicVolumeUpButton.OnClicked.AddListener(OnMusicVolumeUpClicked);

        if (musicVolumeDownButton != null)
            musicVolumeDownButton.OnClicked.AddListener(OnMusicVolumeDownClicked);

        if (sfxVolumeUpButton != null)
            sfxVolumeUpButton.OnClicked.AddListener(OnSFXVolumeUpClicked);

        if (sfxVolumeDownButton != null)
            sfxVolumeDownButton.OnClicked.AddListener(OnSFXVolumeDownClicked);


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

    #region Button Click Handlers (主菜单/返回)

    public void OnStartGameClicked()
    {
        PlayButtonClickSFX();
        sceneController.LoadInGameScene();
    }

    public void OnSettingsClicked()
    {
        PlayButtonClickSFX();
        ShowSettingsPanel();
    }

    public void OnQuitClicked()
    {
        PlayButtonClickSFX();
        sceneController.QuitGame();
    }

    public void OnBackClicked()
    {
        PlayButtonClickSFX();
        ShowMainMenu();
    }

    #endregion

    #region Volume Control Buttons

    public void OnMusicVolumeUpClicked()
    {
        ChangeMusicVolume(VolumeStep);
    }

    public void OnMusicVolumeDownClicked()
    {
        ChangeMusicVolume(-VolumeStep);
    }

    public void OnSFXVolumeUpClicked()
    {
        ChangeSFXVolume(VolumeStep);
    }

    public void OnSFXVolumeDownClicked()
    {
        ChangeSFXVolume(-VolumeStep);
    }

    /// <summary>
    /// 统一处理音乐音量变化 (± step)
    /// </summary>
    private void ChangeMusicVolume(float step)
    {
        if (musicVolumeSlider != null)
        {
            float currentValue = musicVolumeSlider.value;
            float newValue = Mathf.Clamp01(currentValue + step);

            if (newValue == currentValue && AudioManager.Instance != null && step != 0)
            {
                // 如果值没有变化 (达到0或1)，播放错误音效
                if (buttonErrorSFX != null)
                {
                    AudioManager.Instance.PlaySFX(buttonErrorSFX);
                }
                Debug.Log($"[Volume] 音乐音量已达到 {(step > 0 ? "上限" : "下限")}。");
                return;
            }

            // 成功，播放点击音效并更新 Slider
            PlayButtonClickSFX();
            musicVolumeSlider.value = newValue;
        }
    }

    /// <summary>
    /// 统一处理音效音量变化 (± step)
    /// </summary>
    private void ChangeSFXVolume(float step)
    {
        if (sfxVolumeSlider != null)
        {
            float currentValue = sfxVolumeSlider.value;
            float newValue = Mathf.Clamp01(currentValue + step);

            if (newValue == currentValue && AudioManager.Instance != null && step != 0)
            {
                // 如果值没有变化 (达到0或1)，播放错误音效
                if (buttonErrorSFX != null)
                {
                    AudioManager.Instance.PlaySFX(buttonErrorSFX);
                }
                Debug.Log($"[Volume] 音效音量已达到 {(step > 0 ? "上限" : "下限")}。");
                return;
            }

            // 成功，播放点击音效并更新 Slider
            PlayButtonClickSFX();
            sfxVolumeSlider.value = newValue;
        }
    }

    #endregion

    #region Volume Control (滑条拖动和显示更新)

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

    private void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void ShowSettingsPanel()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    #endregion

    #region Audio Feedback

    private void PlayButtonClickSFX()
    {
        if (AudioManager.Instance != null && buttonClickSFX != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSFX);
        }
    }

    #endregion
}