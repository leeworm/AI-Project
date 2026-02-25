using UnityEngine;

public static class AutoBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureApp()
    {
        // 이미 있으면 끝
        if (App.I != null && SceneRouter.I != null)
            return;

        // App/SceneRouter가 붙은 오브젝트를 런타임에 만든다
        var go = new GameObject("App(Auto)");
        go.AddComponent<App>();
        go.AddComponent<SceneRouter>();
        go.AddComponent<AudioHub>();
        go.AddComponent<UIClickSfxInstaller>();
        Object.DontDestroyOnLoad(go);
    }
}
