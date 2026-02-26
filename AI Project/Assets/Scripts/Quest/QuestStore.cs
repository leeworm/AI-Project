using System;
using System.Collections.Generic;

public interface IQuestStore
{
    string Get(string key);
    void Set(string key, string value);
    bool GetBool(string key, bool defaultValue = false);
    void SetBool(string key, bool value);
}

public sealed class QuestStore_AppGlobal : IQuestStore
{
    private readonly NpcState _global;

    public QuestStore_AppGlobal(NpcState globalState)
    {
        _global = globalState ?? throw new ArgumentNullException(nameof(globalState));
    }

    public string Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var idx = _global.flags.FindIndex(f => f.key == key);
        return idx >= 0 ? _global.flags[idx].value : null;
    }

    public void Set(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _global.UpsertFlag(key, value ?? "");
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        var v = Get(key);
        if (v == null) return defaultValue;
        return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public void SetBool(string key, bool value) => Set(key, value ? "1" : "0");
}