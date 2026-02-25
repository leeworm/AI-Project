using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioHub : MonoBehaviour
{
    public static AudioHub I { get; private set; }

    [Header("Config")]
    [SerializeField] private string configResourceName = "AudioConfig_Main";
    private AudioConfig _cfg;

    private AudioSource _bgm;
    private AudioSource _sfx;
    private Coroutine _fadeCo;
    private AudioClip _currentBgm;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        _cfg = Resources.Load<AudioConfig>(configResourceName);
        if (_cfg == null)
        {
            Debug.LogError($"AudioConfig not found in Resources: {configResourceName}.asset");
            return;
        }

        _bgm = gameObject.AddComponent<AudioSource>();
        _bgm.playOnAwake = false;
        _bgm.loop = true;
        _bgm.volume = _cfg.bgmVolume;

        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.loop = false;

        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        // 시작 씬 BGM 적용
        ApplyBgmForScene(SceneManager.GetActiveScene().buildIndex, immediate: true);
    }

    private void OnDestroy()
    {
        if (I == this)
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        ApplyBgmForScene(newScene.buildIndex, immediate: false);
    }

    private void ApplyBgmForScene(int buildIndex, bool immediate)
    {
        if (_cfg == null) return;

        // Options는 BGM 유지
        if (buildIndex == (int)SceneId.Options)
            return;

        var target = GetBgmClip(buildIndex);

        // BGM 지정이 없으면 무음 처리
        if (target == null)
        {
            if (_fadeCo != null) StopCoroutine(_fadeCo);

            float fadeTime = Mathf.Max(0f, _cfg.bgmFadeTime);

            if (immediate || fadeTime <= 0f)
            {
                _bgm.Stop();
                _bgm.clip = null;
                _currentBgm = null;
            }
            else
            {
                _fadeCo = StartCoroutine(FadeOutToSilence(fadeTime));
            }
            return;
        }

        // 이미 같은 곡이면 유지
        if (_currentBgm == target && _bgm.isPlaying)
            return;

        _currentBgm = target;

        if (_fadeCo != null) StopCoroutine(_fadeCo);

        float tFade = Mathf.Max(0f, _cfg.bgmFadeTime);

        if (immediate || tFade <= 0f)
        {
            _bgm.clip = target;
            _bgm.volume = _cfg.bgmVolume;
            _bgm.Play();
            return;
        }

        _fadeCo = StartCoroutine(FadeTo(target, tFade));
    }

    private AudioClip GetBgmClip(int buildIndex)
    {
        // SceneId enum + Build Settings index 일치 전제
        if (buildIndex == (int)SceneId.MainMenu) return _cfg.bgmMainMenu;
        if (buildIndex == (int)SceneId.HomeHub) return _cfg.bgmHomeHub;
        if (buildIndex == (int)SceneId.WorldMap) return _cfg.bgmWorldMap;
        if (buildIndex == (int)SceneId.LocationCafe) return _cfg.bgmCafe;
        return null;
    }

    private IEnumerator FadeTo(AudioClip next, float fadeTime)
    {
        float t = 0f;
        float startVol = _bgm.volume;

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            _bgm.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        _bgm.Stop();
        _bgm.clip = next;
        _bgm.Play();

        t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            _bgm.volume = Mathf.Lerp(0f, _cfg.bgmVolume, t / fadeTime);
            yield return null;
        }

        _bgm.volume = _cfg.bgmVolume;
        _fadeCo = null;
    }
    private IEnumerator FadeOutToSilence(float fadeTime)
    {
        float t = 0f;
        float startVol = _bgm.volume;

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            _bgm.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        _bgm.Stop();
        _bgm.clip = null;
        _bgm.volume = _cfg.bgmVolume; // 다음 곡 재생 대비
        _currentBgm = null;
        _fadeCo = null;
    }

    // ---- SFX ----
    public void PlayUIClick() => PlaySfx(_cfg != null ? _cfg.sfxUiClick : null);
    public void PlayDialogueStart() => PlaySfx(_cfg != null ? _cfg.sfxDialogueStart : null);

    public void PlaySfx(AudioClip clip)
    {
        if (_cfg == null || clip == null) return;
        _sfx.PlayOneShot(clip, _cfg.sfxVolume);
    }
}