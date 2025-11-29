using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// VR UI按钮音效反馈
/// 添加到每个Button上，提供悬停和点击音效
/// </summary>
[RequireComponent(typeof(Button))]
public class VRButtonAudioFeedback : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    [Header("Volume Settings")]
    [SerializeField] [Range(0f, 1f)] private float hoverVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float clickVolume = 0.8f;

    /// <summary>
    /// 当指针进入按钮时（VR射线悬停）
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AudioManager.Instance != null && hoverSound != null)
        {
            AudioManager.Instance.PlaySFX(hoverSound, hoverVolume);
        }
    }

    /// <summary>
    /// 当按钮被点击时
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (AudioManager.Instance != null && clickSound != null)
        {
            AudioManager.Instance.PlaySFX(clickSound, clickVolume);
        }
    }
}