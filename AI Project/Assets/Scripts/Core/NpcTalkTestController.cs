using System;
using TMPro;
using UnityEngine;

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

    private DialogueService _dialogue;

    private void Awake()
    {
        _dialogue = new DialogueService(proxyUrl);
    }

    public async void OnClickSend()
    {
        try
        {
            if (npc == null)
                throw new Exception("NpcDefinition이 연결되지 않았습니다.");

            if (GameBootstrap.State == null)
                throw new Exception("GameBootstrap.State가 null입니다. 씬에 GameBootstrap 오브젝트가 있는지 확인하세요.");

            var playerInput = inputField.text?.Trim();
            if (string.IsNullOrEmpty(playerInput))
                return;

            debugText.text = "Talking...";

            var resp = await _dialogue.TalkAsync(GameBootstrap.State, npc, playerInput);

            npcReplyText.text = resp.reply;

            // 상태 표시(저장된 실제 값)
            var npcState = GameBootstrap.State.GetOrCreateNpc(npc.npcId);
            debugText.text =
                $"npc={npc.displayName}\n" +
                $"affinity={npcState.affinity}\n" +
                $"flags={npcState.flags.Count}\n" +
                $"turns={npcState.recentTurns.Count}\n" +
                $"memo={npcState.summaryMemo}";

            inputField.text = "";
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            debugText.text = "Error:\n" + e.Message;
        }
    }
}
