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

    public Task<NpcTalkResponse> TalkAsync(NpcDefinition def, string playerInput)
    {
        // 기존 호출부(윤서) 깨지지 않도록 유지
        return TalkAsync(def, playerInput, forceGpt: false);
    }

    public async Task<NpcTalkResponse> TalkAsync(NpcDefinition def, string playerInput, bool forceGpt)
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

        // forceGpt면 무조건 프록시 호출
        bool callGpt = forceGpt || ShouldCallGpt(playerInput);

        if (!callGpt)
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

            Debug.Log("[PROXY RAW]\n" + raw);
            Debug.Log("[GPT RAW]\n" + resp.reply);

            // 태그 방식(있으면 파싱) + note 폴백
            resp.reply = DynamicQuestIngestor.IngestAndStrip(resp.reply);
            DynamicQuestIngestor.TryApplyFromNote(resp.reply, resp.note);

            Debug.Log("[GPT CLEAN]\n" + resp.reply);
            Debug.Log("[DQ title] " + App.I.GetGlobalFlag("quest.dynamic_generated.title"));

            ApplyResponseToState(npc, resp);
        }

        // 공통 자동 저장(성공적으로 상태 반영이 끝난 뒤)
        app.Save();

        return resp;
    }

    private static NpcTalkRequest BuildRequest(WorldState world, NpcDefinition def, NpcState npc, string playerInput)
    {
        const string QUEST_JSON_RULES =
        "\n\n[출력 규칙 - 시스템용 퀘스트 JSON]\n" +
        "- 먼저 자연어 답변을 1~3문장으로 작성합니다.\n" +
        "- 답변 맨 마지막에 반드시 아래 태그 2개와 JSON '한 줄'을 추가합니다.\n" +
        "- JSON은 코드블록(```) 금지, 백틱 금지, 추가 설명 금지.\n" +
        "- 키 이름은 id/title/objective/targetNpcId 정확히 유지.\n" +
        "- id는 반드시 \"dynamic_generated\".\n" +
        "- targetNpcId는 반드시 소문자 토큰 하나만: yoonseo 또는 harin 또는 sea (기호 | / , 사용 금지)\n" +
        "QUEST_JSON_START\n" +
        "{\"id\":\"dynamic_generated\",\"title\":\"프로젝트 기획하기\",\"objective\":\"하린에게 조언 구하기\",\"targetNpcId\":\"harin\"}\n" +
        "QUEST_JSON_END\n";

        string persona = def.persona;

        // 데모용: 윤서 응답에는 항상 퀘스트 JSON 태그를 붙이도록 강제(성공률 최대)
        if (string.Equals(def.npcId, "yoonseo", StringComparison.OrdinalIgnoreCase))
        {
            persona += QUEST_JSON_RULES;
        }

        return new NpcTalkRequest
        {
            npc_id = def.npcId,
            npc_name = def.displayName,
            persona = persona,

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
            "morning" => "좋은 아침이네요.",
            "afternoon" => "점심 맛있게 드세요.",
            "evening" => "좋은 저녁 보내세요.",
            _ => "지금은 무리하지 않는 게 좋겠습니다."
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

        string[] triggers = { "퀘스트", "선택", "결정", "왜", "어떻게", "도와", "계획", "비밀", "중요", "어떤", "할 일", "누구" };
        foreach (var t in triggers)
            if (playerInput.Contains(t, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
}
