using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private int slot = 1;
    public int Slot => slot;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("persistentDataPath: " + Application.persistentDataPath);
    }

    [Serializable]
    private class NpcsSaveData
    {
        public List<NpcState> npcs = new();
    }

    public void SaveAll(GameState state)
    {
        if (state == null) return;

        var slotDir = GetSlotDir(slot);
        if (!Directory.Exists(slotDir))
            Directory.CreateDirectory(slotDir);

        // World
        File.WriteAllText(Path.Combine(slotDir, "world.json"), JsonUtility.ToJson(state.World, true));

        // NPCs
        var npcsData = new NpcsSaveData();
        foreach (var kv in state.AllNpcs)
            npcsData.npcs.Add(kv.Value);

        File.WriteAllText(Path.Combine(slotDir, "npcs.json"), JsonUtility.ToJson(npcsData, true));
    }

    public void LoadAll(GameState state)
    {
        if (state == null) return;

        var slotDir = GetSlotDir(slot);

        // World
        var worldPath = Path.Combine(slotDir, "world.json");
        if (File.Exists(worldPath))
        {
            var json = File.ReadAllText(worldPath);
            state.ReplaceWorld(JsonUtility.FromJson<WorldState>(json));
        }
        else
        {
            state.ReplaceWorld(new WorldState());
        }

        // NPCs
        var npcsPath = Path.Combine(slotDir, "npcs.json");
        if (File.Exists(npcsPath))
        {
            var json = File.ReadAllText(npcsPath);
            var npcs = JsonUtility.FromJson<NpcsSaveData>(json);
            state.ReplaceAllNpcs(npcs?.npcs);
        }
        else
        {
            state.ReplaceAllNpcs(null);
        }
    }

    private static string GetSlotDir(int slot)
    {
        return Path.Combine(Application.persistentDataPath, "Save", $"slot_{slot:00}");
    }
}
