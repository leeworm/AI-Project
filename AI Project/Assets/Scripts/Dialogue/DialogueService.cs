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

    public async Task<NpcTalkResponse> TalkAsync(GameState game, NpcDefinition def, string playerInput)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (def == null) throw new ArgumentNullException(nameof(def));

        playerInput = playerInput?.Trim();
        if (string.IsNullOrEmpty(playerInput))
            throw new Exception("playerInput is empty.");

        var npc = game.GetOrCreateNpc(def.npcId);

        // 최근 대화에 플레이어 입력 먼저 기록
        npc.AddTurn("player", playerInput);

        NpcTalkResponse resp;

        // ✅ 로컬/프록시 분기
        if (!DialogueCallPolicy.ShouldCallGpt(playerInput))
        {
            resp = BuildLocalReply(game, def, npc, playerInput);
            ApplyResponseToState(npc, resp);
        }
        else
        {
            var req = BuildRequest(game, def, npc, playerInput);
            var raw = await _client.PostJsonAsync(JsonUtility.ToJson(req));
            resp = JsonUtility.FromJson<NpcTalkResponse>(raw);

            if (resp == null || string.IsNullOrWhiteSpace(resp.reply))
                throw new Exception("Invalid response from proxy.");

            ApplyResponseToState(npc, resp);
        }

        // ✅ 공통 자동 저장(성공적으로 상태 반영이 끝난 뒤)
        SaveManager.Instance?.SaveAll(GameBootstrap.State);

        return resp;
    }

    private static NpcTalkRequest BuildRequest(GameState game, NpcDefinition def, NpcState npc, string playerInput)
    {
        return new NpcTalkRequest
        {
            npc_id = def.npcId,
            npc_name = def.displayName,
            persona = def.persona,

            day = game.World.day,
            time_slot = game.World.timeSlot,

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

        // 요약 메모(초간단): note로 갱신
        if (!string.IsNullOrWhiteSpace(resp.note))
            npc.summaryMemo = resp.note;
    }

    private static NpcTalkResponse BuildLocalReply(GameState game, NpcDefinition def, NpcState npc, string playerInput)
    {
        // 최소 로컬 템플릿(바이브 코딩 친화)
        string reply = game.World.timeSlot switch
        {
            "morning" => "안녕하세요. 오늘 오전 일정부터 정리하겠습니다. 무엇을 우선할까요?",
            "afternoon" => "오후에는 처리할 일이 늘어납니다. 우선순위를 말씀해 주세요.",
            "evening" => "저녁입니다. 오늘 남은 과제를 마무리할지, 정리하고 쉬실지 결정하셔야 합니다.",
            _ => "지금은 무리하지 않는 게 좋겠습니다. 가장 필요한 것부터 말해 주세요."
        };

        return new NpcTalkResponse
        {
            reply = reply,
            affinity_delta = 0,
            flag_updates = new List<FlagKV>(), // 스키마 일관성 유지
            note = "로컬 응답 사용"
        };
    }
}
