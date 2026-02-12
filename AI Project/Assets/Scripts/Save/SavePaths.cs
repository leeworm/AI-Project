using System.IO;
using UnityEngine;

public static class SavePaths
{
    public static string SlotDir(int slot)
        => Path.Combine(Application.persistentDataPath, "Save", $"slot_{slot:00}");

    public static string WorldPath(int slot)
        => Path.Combine(SlotDir(slot), "world.json");

    public static string NpcsPath(int slot)
        => Path.Combine(SlotDir(slot), "npcs.json");
}
