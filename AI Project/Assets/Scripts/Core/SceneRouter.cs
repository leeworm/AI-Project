using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRouter : MonoBehaviour
{
    public static SceneRouter I { get; private set; }

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // Boot 씬에서 자동으로 메뉴로 보내고 싶으면:
        SceneManager.LoadScene((int)SceneId.MainMenu);
    }

    public void Continue(int slot = 1)
    {
        App.I.SetSlot(slot);

        if (!App.I.HasSave(slot))
        {
            Debug.LogWarning("No save found.");
            // 필요하면 MainMenu에 남기거나 안내 UI 띄우기
            return;
        }

        App.I.LoadOrInit();
        var idx = App.I.Route.scene_build_index;
        if (idx < 0 || idx >= SceneManager.sceneCountInBuildSettings)
            idx = (int)SceneId.HomeHub;
        SceneManager.LoadScene(idx);
    }

    public void NewGame(int slot = 1)
    {
        App.I.NewGame(slot);
        SceneManager.LoadScene(App.I.Route.scene_build_index);
    }
    public void StartNewGameSlot(int slot)
    {
        App.I.NewGame(slot);
        SceneManager.LoadScene(App.I.Route.scene_build_index);
    }

    public void Go(SceneId id, string spawn = SpawnPointIds.EntryDefault)
    {
        App.I.Route.scene_build_index = (int)id;
        App.I.Route.spawn_point = spawn;
        App.I.Save();
        SceneManager.LoadScene((int)id);
    }

    public void UI_Continue_Slot1()
    {
        Continue(1);
    }

    public void UI_NewGame_Slot1()
    {
        NewGame(1);
    }

    public void UI_Options()
    {
        Go(SceneId.Options);
    }

    public void UI_Quit()
    {
#if UNITY_EDITOR
        Debug.Log("Quit (Editor)");
#else
    Application.Quit();
#endif
    }

}
