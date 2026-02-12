using System;
using System.Collections.Generic;

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
        recentTurns.Add(new Turn { speaker = speaker, text = text });
        if (recentTurns.Count > maxTurns)
            recentTurns.RemoveRange(0, recentTurns.Count - maxTurns);
    }

    public void UpsertFlag(string key, string value)
    {
        var idx = flags.FindIndex(f => f.key == key);
        if (idx >= 0) flags[idx].value = value;
        else flags.Add(new FlagKV { key = key, value = value });
    }
}

[Serializable]
public class Turn
{
    public string speaker;
    public string text;
}
