using System;

public interface IQuestEvent { }

public readonly struct TalkEvent : IQuestEvent
{
    public readonly string NpcId;
    public readonly string LocationId; // "cafe" 같은 식으로(없으면 "")

    public TalkEvent(string npcId, string locationId)
    {
        NpcId = npcId ?? "";
        LocationId = locationId ?? "";
    }
}

public readonly struct EnterLocationEvent : IQuestEvent
{
    public readonly string LocationId;

    public EnterLocationEvent(string locationId)
    {
        LocationId = locationId ?? "";
    }
}