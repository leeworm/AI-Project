using System;
using System.Collections.Generic;

public enum SceneId
{
    Boot = 0,
    MainMenu = 1,
    Options = 2,
    Prologue = 3,
    HomeHub = 4,
    WorldMap = 5,
    LocationCafe = 6,
    DemoDay = 7,
}

public static class SpawnPointIds
{
    public const string EntryDefault = "Entry_Default";
    public const string FromWorldMap = "From_WorldMap";
}

[Serializable]
public class SaveData
{
    public int save_version = 1;
    public string saved_at = "";

    public RouteState route = new RouteState();
    public WorldState world = new WorldState();
    public List<NpcState> npcs = new List<NpcState>();
}

[Serializable]
public class RouteState
{
    public int scene_build_index;
    public string spawn_point;
}

[Serializable]
public class WorldState
{
    public int day;
    public string timeSlot;
    public string locationId;
}

[Serializable]
public class FlagKV
{
    public string key;
    public string value;
}

[Serializable]
public class Turn
{
    public string speaker;
    public string text;
}

[Serializable]
public class NpcState
{
    public string npcId;
    public int affinity;
    public List<FlagKV> flags = new();
    public string summaryMemo = "";
    public List<Turn> recentTurns = new();

    public void AddTurn(string speaker, string text, int maxTurns = 10)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        recentTurns.Add(new Turn { speaker = speaker, text = text });

        if (recentTurns.Count > maxTurns)
            recentTurns.RemoveRange(0, recentTurns.Count - maxTurns);
    }

    public void UpsertFlag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        var idx = flags.FindIndex(f => f.key == key);
        if (idx >= 0) flags[idx].value = value;
        else flags.Add(new FlagKV { key = key, value = value });
    }
}

