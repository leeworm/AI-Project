using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Npc Database")]
public class NpcDatabase : ScriptableObject
{
    public List<NpcDefinition> npcs = new();

    public NpcDefinition Find(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId)) return null;
        return npcs.Find(n => n != null && n.npcId == npcId);
    }
}
