using System;
using System.Collections.Generic;

[Serializable]
public class NpcTalkRequest
{
    public string npc_id;
    public string npc_name;
    public string persona;

    public int day;
    public string time_slot;

    public int affinity;
    public string player_input;

    public List<FlagKV> flags = new();
}

[Serializable]
public class FlagKV
{
    public string key;
    public string value;
}

[Serializable]
public class NpcTalkResponse
{
    public string reply;
    public int affinity_delta;
    public List<FlagKV> flag_updates;
    public string note;
}
