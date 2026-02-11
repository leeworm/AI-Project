using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NpcTalkTestController : MonoBehaviour
{
    [Header("Proxy")]
    [SerializeField] private string proxyUrl = "http://127.0.0.1:5000/api/npc-talk";

    [Header("NPC")]
    [SerializeField] private NpcDefinition npc;

    [Header("UI")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text npcReplyText;
    [SerializeField] private TMP_Text debugText;

    private NpcProxyClient _client;
    private int _day = 1;
    private string _timeSlot = "morning";
    private int _affinity = 0;

    private readonly List<FlagKV> _flags = new();

    private void Awake()
    {
        _client = new NpcProxyClient(proxyUrl);
    }

    public async void OnClickSend()
    {
        try
        {
            var playerInput = inputField.text?.Trim();
            if (string.IsNullOrEmpty(playerInput))
                return;

            debugText.text = "Calling proxy...";

            var req = new NpcTalkRequest
            {
                npc_id = npc != null ? npc.npcId : "npc",
                npc_name = npc != null ? npc.displayName : "NPC",
                persona = npc != null ? npc.persona : "",
                day = _day,
                time_slot = _timeSlot,
                affinity = _affinity,
                player_input = playerInput,
                flags = new List<FlagKV>(_flags)
            };

            // Unity JsonUtility는 List 직렬화는 되지만, 복잡한 케이스에서 제약이 있어요.
            // 지금은 "단순 테스트"라 JsonUtility로 갑니다.
            var json = JsonUtility.ToJson(req);

            var raw = await _client.PostJsonAsync(json);

            // 지금 프록시는 OpenAI Responses API의 "원본 JSON 전체"를 반환하고 있습니다.
            // 우리는 그 안에서 "schema에 맞는 JSON"만 꺼내야 하는데,
            // 첫 성공 단계에서는 프록시 응답을 그대로 콘솔에 찍고 확인하는 방식으로 갑니다.
            // (다음 단계에서 프록시가 reply JSON만 추출해서 내려주게 개선합니다.)
            Debug.Log(raw);

            // 임시: 프록시가 "딱 response JSON만" 내려준다고 가정하고 파싱 시도
            // (파싱 실패하면 debugText에 원인 표시)
            var parsed = JsonUtility.FromJson<NpcTalkResponse>(raw);

            npcReplyText.text = parsed.reply;
            _affinity += parsed.affinity_delta;

            if (parsed.flag_updates != null)
            {
                foreach (var kv in parsed.flag_updates)
                    UpsertFlag(kv.key, kv.value);
            }

            debugText.text = $"affinity={_affinity}\nnote={parsed.note}\nflags={_flags.Count}";
            inputField.text = "";
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            debugText.text = "Error:\n" + e.Message;
        }
    }

    private void UpsertFlag(string key, string value)
    {
        var idx = _flags.FindIndex(f => f.key == key);
        if (idx >= 0) _flags[idx].value = value;
        else _flags.Add(new FlagKV { key = key, value = value });
    }
}
