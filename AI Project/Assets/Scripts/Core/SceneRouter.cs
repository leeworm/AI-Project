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

    public void Go(SceneId id, string spawn = SpawnPointIds.EntryDefault)
    {
        App.I.Route.scene_build_index = (int)id;
        App.I.Route.spawn_point = spawn;
        App.I.Save();
        SceneManager.LoadScene((int)id);
    }
}
