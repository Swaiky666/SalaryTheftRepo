using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景管理器
/// 负责场景加载和切换
/// </summary>
public class SceneController : MonoBehaviour
{
    /// <summary>
    /// 加载指定场景
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// **新增：初始化游戏进度并加载场景**
    /// 确保在开始新游戏时调用此方法，以重置 PlayerPrefs 中的职位和难度。
    /// </summary>
    /// <param name="sceneName">要加载的目标场景名称 (e.g., "InGame" 或 "Tutorial")</param>
    public void InitializeNewGameAndLoad(string sceneName)
    {
        // 1. 调用 GameLogicSystem 静态方法重置持久化数据
        GameLogicSystem.ResetGameProgress();

        // 2. 加载目标场景
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 从主菜单开始新游戏时调用此方法，以确保初始化。
    /// 加载InGame场景
    /// </summary>
    public void LoadInGameScene()
    {
        InitializeNewGameAndLoad("InGame");
    }

    /// <summary>
    /// 从主菜单开始新游戏时调用此方法，以确保初始化。
    /// 加载Tutorial场景
    /// </summary>
    public void LoadTutorialScene()
    {
        InitializeNewGameAndLoad("Tutorial");
    }

    /// <summary>
    /// 加载StartMenu场景 (失败后返回主菜单)
    /// </summary>
    public void LoadStartMenuScene()
    {
        SceneManager.LoadScene("StartMenu");
    }

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}