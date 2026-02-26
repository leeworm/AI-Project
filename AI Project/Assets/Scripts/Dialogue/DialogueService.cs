using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DialogueService
{
    private readonly NpcProxyClient _client;

    public DialogueService(string proxyUrl)
    {
        _client = new NpcProxyClient(proxyUrl);
    }

    public async Task<NpcTalkResponse> TalkAsync(NpcDefinition def, string playerInput)
    {
        if (App.I == null)
            throw new InvalidOperationException("App is not initialized. Boot 씬에 App 오브젝트가 있는지 확인하세요.");

        if (def == null)
            throw new ArgumentNullException(nameof(def));

        playerInput = playerInput?.Trim();
        if (string.IsNullOrEmpty(playerInput))
            throw new Exception("playerInput is empty.");

        var app = App.I;

        if (!app.Npcs.TryGetValue(def.npcId, out var npc))
        {
            npc = new NpcState { npcId = def.npcId, affinity = 0 };
            app.Npcs[def.npcId] = npc;
        }

        // 최근 대화에 플레이어 입력 먼저 기록
        npc.AddTurn("player", playerInput);

        NpcTalkResponse resp;

        // 로컬/프록시 분기
        if (!ShouldCallGpt(playerInput))
        {
            resp = BuildLocalReply(app.World, def, npc, playerInput);
            ApplyResponseToState(npc, resp);
        }
        else
        {
            var req = BuildRequest(app.World, def, npc, playerInput);
            var raw = await _client.PostJsonAsync(JsonUtility.ToJson(req));
            resp = JsonUtility.FromJson<NpcTalkResponse>(raw);

            if (resp == null || string.IsNullOrWhiteSpace(resp.reply))
                throw new Exception("Invalid response from proxy.");

            ApplyResponseToState(npc, resp);
        }

        // 공통 자동 저장(성공적으로 상태 반영이 끝난 뒤)
        app.Save();

        return resp;
    }

    private static NpcTalkRequest BuildRequest(WorldState world, NpcDefinition def, NpcState npc, string playerInput)
    {
        return new NpcTalkRequest
        {
            npc_id = def.npcId,
            npc_name = def.displayName,
            persona = def.persona,

            day = world.day,
            time_slot = world.timeSlot,

            affinity = npc.affinity,
            flags = npc.flags,

            recent_turns = npc.recentTurns,
            summary_memo = npc.summaryMemo,

            player_input = playerInput
        };
    }

    private static void ApplyResponseToState(NpcState npc, NpcTalkResponse resp)
    {
        npc.affinity += resp.affinity_delta;

        if (resp.flag_updates != null)
        {
            foreach (var kv in resp.flag_updates)
                npc.UpsertFlag(kv.key, kv.value);
        }

        // NPC 응답 기록
        npc.AddTurn("npc", resp.reply);

        // 요약 메모: note로 갱신
        if (!string.IsNullOrWhiteSpace(resp.note))
            npc.summaryMemo = resp.note;
    }

    private static NpcTalkResponse BuildLocalReply(WorldState world, NpcDefinition def, NpcState npc, string playerInput)
    {
        string reply = world.timeSlot switch
        {
            "morning" => "좋은 아침입니다.",
            "afternoon" => "점심 맛있게 드세요.",
            "evening" => "좋은 저녁 보내세요.",
            _ => "지금은 무리하지 않는 게 좋겠습니다. 가장 필요한 것부터 말해 주세요."
        };

        return new NpcTalkResponse
        {
            reply = reply,
            affinity_delta = 0,
            flag_updates = new List<FlagKV>(),
            note = "로컬 응답 사용"
        };
    }

    private static bool ShouldCallGpt(string playerInput)
    {
        if (string.IsNullOrWhiteSpace(playerInput)) return false;

        string[] triggers = { "퀘스트", "선택", "결정", "왜", "어떻게", "도와", "계획", "비밀", "중요", "어떤" };
        foreach (var t in triggers)
            if (playerInput.Contains(t, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
}
