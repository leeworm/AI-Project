using System.Collections.Generic;

public class GameState
{
    public WorldState World { get; private set; } = new WorldState();

    // npcId -> state (Dictionary는 JsonUtility로 저장 어렵기 때문에 저장은 리스트로 처리)
    private readonly Dictionary<string, NpcState> _npcStates = new();

    public NpcState GetOrCreateNpc(string npcId)
    {
        if (_npcStates.TryGetValue(npcId, out var s))
            return s;

        s = new NpcState { npcId = npcId, affinity = 0 };
        _npcStates[npcId] = s;
        return s;
    }

    public IReadOnlyDictionary<string, NpcState> AllNpcs => _npcStates;

    public void ReplaceWorld(WorldState world) => World = world ?? new WorldState();

    public void ReplaceAllNpcs(IEnumerable<NpcState> npcStates)
    {
        _npcStates.Clear();
        if (npcStates == null) return;

        foreach (var s in npcStates)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.npcId)) continue;
            _npcStates[s.npcId] = s;
        }
    }
}
