using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private int slot = 1;

    public int Slot => slot;
    public const int MinSlot = 1;
    public const int MaxSlot = 3;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSlot(int slotIndex)
    {
        slot = Mathf.Clamp(slotIndex, MinSlot, MaxSlot);
    }

    public bool HasSaveInSlot(int slotIndex)
    {
        var dir = GetSlotDir(slotIndex);
        var path = Path.Combine(dir, "save.json");
        return File.Exists(path);
    }

    public void DeleteSlot(int slotIndex)
    {
        var dir = GetSlotDir(slotIndex);
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
    }

    public void SaveAll(GameState state)
    {
        if (state == null) return;

        var dir = GetSlotDir(slot);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var save = new SaveData
        {
            saved_at = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            route = state.Route,
            world = state.World,
            npcs = new List<NpcState>()
        };

        foreach (var kv in state.AllNpcs)
            save.npcs.Add(kv.Value);

        var json = JsonUtility.ToJson(save, true);
        File.WriteAllText(Path.Combine(dir, "save.json"), json);
    }

    public bool LoadAll(GameState state)
    {
        if (state == null) return false;

        var dir = GetSlotDir(slot);
        var path = Path.Combine(dir, "save.json");
        if (!File.Exists(path))
            return false;

        var json = File.ReadAllText(path);
        var save = JsonUtility.FromJson<SaveData>(json);
        if (save == null) return false;

        state.ReplaceWorld(save.world);
        state.ReplaceRoute(save.route);
        state.ReplaceAllNpcs(save.npcs);

        return true;
    }

    private static string GetSlotDir(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, "Save", $"slot_{slotIndex:00}");
    }
}
