using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIClickSfxInstaller : MonoBehaviour
{
    private static readonly HashSet<int> _installed = new HashSet<int>();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        InstallForLoadedScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallForLoadedScene();
    }

    private static void InstallForLoadedScene()
    {
        // Unity 6 권장 API
        var buttons = Object.FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            var id = btn.GetInstanceID();
            if (_installed.Contains(id))
                continue;

            _installed.Add(id);

            btn.onClick.AddListener(() => AudioHub.I?.PlayUIClick());
        }
    }
}