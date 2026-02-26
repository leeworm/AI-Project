using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class UIManager : MonoBehaviour
{
    public static UIManager I { get; private set; }

    [Header("HUD Root (Quest panel lives here)")]
    [SerializeField] private GameObject inGameHudRoot;

    [Header("Direct refs (optional)")]
    [SerializeField] private QuestPanelController questPanel;

    public QuestPanelController QuestPanel => questPanel;
    
    [Header("Scenes where HUD should be hidden")]
    [SerializeField]
    private SceneId[] hideHudInScenes =
    {
        SceneId.MainMenu,
        SceneId.Options
    };

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // 인스펙터로 안 넣었으면 자동 탐색
        if (questPanel == null)
            questPanel = GetComponentInChildren<QuestPanelController>(true);

        // UIRoot가 DDOL이므로 씬 변경을 추적
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        Apply(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene from, Scene to)
    {
        Apply(to.buildIndex);
    }

    private void Apply(int buildIndex)
    {
        bool hideHud = false;

        for (int i = 0; i < hideHudInScenes.Length; i++)
        {
            if (buildIndex == (int)hideHudInScenes[i])
            {
                hideHud = true;
                break;
            }
        }

        if (inGameHudRoot != null)
            inGameHudRoot.SetActive(!hideHud);
    }
}