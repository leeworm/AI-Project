using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class App : MonoBehaviour
{
    public static App I { get; private set; }

    [SerializeField] private int slot = 1;

    public WorldState World { get; private set; } = new WorldState();
    public RouteState Route { get; private set; } = new RouteState();
    public Dictionary<string, NpcState> Npcs { get; private set; } = new();

    private string SaveDir => Path.Combine(Application.persistentDataPath, "Save", $"slot_{slot:00}");
    private string SavePath => Path.Combine(SaveDir, "save.json");

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        LoadOrInit();
    }

    public void SetSlot(int s) => slot = Mathf.Clamp(s, 1, 3);

    public void NewGame(int s)
    {
        SetSlot(s);
        DeleteSlot();
        InitDefaults();
        Save();
    }

    public bool LoadOrInit()
    {
        if (File.Exists(SavePath))
            return Load();

        InitDefaults();
        Save();
        return false;
    }

    private void InitDefaults()
    {
        World = new WorldState { day = 1, timeSlot = "morning", locationId = "home" };
        Route = new RouteState { scene_build_index = (int)SceneId.MainMenu, spawn_point = SpawnPointIds.EntryDefault };
        Npcs = new Dictionary<string, NpcState>();
    }

    public void Save()
    {
        if (!Directory.Exists(SaveDir))
            Directory.CreateDirectory(SaveDir);

        var save = new SaveData
        {
            save_version = 1,
            saved_at = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            route = Route,
            world = World,
            npcs = new List<NpcState>(Npcs.Values)
        };

        File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
    }

    public bool Load()
    {
        if (!File.Exists(SavePath)) return false;

        var json = File.ReadAllText(SavePath);
        var save = JsonUtility.FromJson<SaveData>(json);
        if (save == null) return false;

        World = save.world ?? new WorldState();
        Route = save.route ?? new RouteState();
        Npcs = new Dictionary<string, NpcState>();

        if (save.npcs != null)
        {
            foreach (var n in save.npcs)
                if (n != null && !string.IsNullOrWhiteSpace(n.npcId))
                    Npcs[n.npcId] = n;
        }

        return true;
    }

    public void DeleteSlot()
    {
        if (Directory.Exists(SaveDir))
            Directory.Delete(SaveDir, true);
    }

#if UNITY_EDITOR
    private void OnDisable() => Save();
#endif
    private void OnApplicationQuit() => Save();
}
