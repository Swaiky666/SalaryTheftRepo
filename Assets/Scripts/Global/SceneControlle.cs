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
    /// 加载InGame场景
    /// </summary>
    public void LoadInGameScene()
    {
        SceneManager.LoadScene("InGame");
    }

    /// <summary>
    /// 加载StartMenu场景
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