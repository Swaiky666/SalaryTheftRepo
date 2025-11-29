using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局音频管理器
/// 管理背景音乐和音效的播放与音量控制
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Background Music Settings")]
    [SerializeField] private List<AudioClip> backgroundMusicList = new List<AudioClip>();
    [SerializeField] private bool shuffleMusic = false;
    private int currentMusicIndex = 0;

    [Header("Volume Settings")]
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.8f;

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 开始播放背景音乐
        if (backgroundMusicList.Count > 0)
        {
            PlayNextMusic();
        }
    }

    /// <summary>
    /// 初始化音频源
    /// </summary>
    private void InitializeAudioSources()
    {
        // 如果没有指定音频源，自动创建
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = false; // 不循环单首，由我们控制播放列表
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // 应用初始音量
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
    }

    private void Update()
    {
        // 检查背景音乐是否播放完毕，自动播放下一首
        if (backgroundMusicList.Count > 0 && !musicSource.isPlaying)
        {
            PlayNextMusic();
        }
    }

    #region Background Music Control

    /// <summary>
    /// 播放下一首背景音乐
    /// </summary>
    private void PlayNextMusic()
    {
        if (backgroundMusicList.Count == 0) return;

        if (shuffleMusic)
        {
            // 随机播放
            currentMusicIndex = Random.Range(0, backgroundMusicList.Count);
        }
        else
        {
            // 顺序播放
            currentMusicIndex = (currentMusicIndex + 1) % backgroundMusicList.Count;
        }

        musicSource.clip = backgroundMusicList[currentMusicIndex];
        musicSource.Play();
    }

    /// <summary>
    /// 设置背景音乐列表（用于不同场景）
    /// </summary>
    public void SetMusicPlaylist(List<AudioClip> newPlaylist)
    {
        if (newPlaylist == null || newPlaylist.Count == 0) return;

        backgroundMusicList = newPlaylist;
        currentMusicIndex = 0;
        PlayNextMusic();
    }

    /// <summary>
    /// 添加音乐到播放列表
    /// </summary>
    public void AddMusicToPlaylist(AudioClip clip)
    {
        if (clip != null && !backgroundMusicList.Contains(clip))
        {
            backgroundMusicList.Add(clip);
        }
    }

    /// <summary>
    /// 设置音乐音量
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }

    /// <summary>
    /// 获取音乐音量
    /// </summary>
    public float GetMusicVolume()
    {
        return musicVolume;
    }

    /// <summary>
    /// 暂停/恢复背景音乐
    /// </summary>
    public void ToggleMusicPause()
    {
        if (musicSource.isPlaying)
            musicSource.Pause();
        else
            musicSource.UnPause();
    }

    #endregion

    #region Sound Effects Control

    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 播放音效（带音量参数）
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }

    /// <summary>
    /// 获取音效音量
    /// </summary>
    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 停止所有音频
    /// </summary>
    public void StopAllAudio()
    {
        musicSource.Stop();
        sfxSource.Stop();
    }

    /// <summary>
    /// 静音/取消静音所有音频
    /// </summary>
    public void MuteAll(bool mute)
    {
        musicSource.mute = mute;
        sfxSource.mute = mute;
    }

    #endregion
}