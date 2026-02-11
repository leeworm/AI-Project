using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Npc Definition")]
public class NpcDefinition : ScriptableObject
{
    public string npcId = "yoonseo";
    public string displayName = "윤서";

    [TextArea] public string persona; // 성격/말투/금기사항 등
}
