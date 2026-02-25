using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio Config")]
public class AudioConfig : ScriptableObject
{
    [Header("BGM Clips")]
    public AudioClip bgmMainMenu;
    public AudioClip bgmHomeHub;
    public AudioClip bgmWorldMap;
    public AudioClip bgmCafe;

    [Header("SFX Clips")]
    public AudioClip sfxUiClick;
    public AudioClip sfxDialogueStart;

    [Header("Default Volumes")]
    [Range(0f, 1f)] public float bgmVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    [Header("BGM Fade")]
    public float bgmFadeTime = 0.35f;
}