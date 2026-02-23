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
        InitDefaultsForNewGame();
        Save();
    }

    public bool LoadOrInit()
    {
        if (File.Exists(SavePath))
            return Load();

        InitDefaultsForBoot();
        return false;
    }

    private void InitDefaultsForBoot()
    {
        World = new WorldState { day = 1, timeSlot = "morning", locationId = "home" };
        Route = new RouteState { scene_build_index = (int)SceneId.MainMenu, spawn_point = SpawnPointIds.EntryDefault };
        Npcs = new Dictionary<string, NpcState>();
    }

    private void InitDefaultsForNewGame()
    {
        World = new WorldState { day = 1, timeSlot = "morning", locationId = "home" };

        // 새 게임 시작 씬을 여기서 고정
        Route = new RouteState
        {
            scene_build_index = (int)SceneId.Prologue,   // 또는 HomeHub
            spawn_point = SpawnPointIds.EntryDefault
        };

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
    public bool HasSave(int s)
    {
        int prev = slot;
        SetSlot(s);
        bool exists = System.IO.File.Exists(SavePath);
        slot = prev;
        return exists;
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

    public void DeleteSlot(int s)
    {
        int prev = slot;
        SetSlot(s);
        DeleteSlot();
        slot = prev;
    }

#if UNITY_EDITOR
    private void OnDisable() => Save();
#endif
    private void OnApplicationQuit() => Save();
}
