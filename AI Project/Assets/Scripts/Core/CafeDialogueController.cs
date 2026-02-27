using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CafeDialogueController : MonoBehaviour
{
    [Header("Proxy")]
    [SerializeField] private string proxyUrl = "http://127.0.0.1:5000/api/npc-talk";

    [Header("NPC Definitions (3명)")]
    [SerializeField] private NpcDefinition npcYoonseo;
    [SerializeField] private NpcDefinition npcHarin;
    [SerializeField] private NpcDefinition npcSea;

    [Header("UI - NPC Buttons")]
    [SerializeField] private Button btnYoonseo;
    [SerializeField] private Button btnHarin;
    [SerializeField] private Button btnSea;

    [Header("UI - Dialogue Panel")]
    [SerializeField] private DialoguePanelController dialoguePanel;

    private DialogueService _dialogue;
    private NpcDefinition _currentNpc;

    private QuestPanelController _questPanel;

    private void Awake()
    {
        _dialogue = new DialogueService(proxyUrl);

        if (btnYoonseo != null) btnYoonseo.onClick.AddListener(() => OpenWith(npcYoonseo));
        if (btnHarin != null) btnHarin.onClick.AddListener(() => OpenWith(npcHarin));
        if (btnSea != null) btnSea.onClick.AddListener(() => OpenWith(npcSea));

        if (dialoguePanel != null)
            dialoguePanel.OnPlayerSubmit += OnPlayerSubmit;

        if (dialoguePanel != null)
            dialoguePanel.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (dialoguePanel != null)
            dialoguePanel.OnPlayerSubmit -= OnPlayerSubmit;
    }
    private void Start()
    {
        if (UIManager.I == null || UIManager.I.QuestPanel == null)
        {
            Debug.LogError("[CafeDialogue] UIManager/QuestPanel not found. Boot 씬으로 실행 중인지 확인하세요.");
            return;
        }

        _questPanel = UIManager.I.QuestPanel;

        // 예: 대화 시작 시 퀘스트 패널 펼치기/접기 등
        // _questPanel.Open();
        // _questPanel.Close();
        // _questPanel.Toggle();
    }

    private void OpenWith(NpcDefinition npc)
    {
        Debug.Log($"OpenWith: {npc.npcId}");
        Debug.Log($"DialoguePanel active before: {dialoguePanel.gameObject.activeSelf}");

        if (npc == null)
        {
            Debug.LogError("NpcDefinition이 연결되지 않았습니다.");
            return;
        }

        _currentNpc = npc;

        dialoguePanel.gameObject.SetActive(true);
        Debug.Log($"DialoguePanel active after: {dialoguePanel.gameObject.activeSelf}");
        AudioHub.I?.PlayDialogueStart();

        // 첫 줄: NPC별로 다르게(윤서는 메인, 나머지는 간단)
        dialoguePanel.ShowNpcLine(GetIntroLine(npc.npcId, npc.displayName));
    }

    private async void OnPlayerSubmit(string playerText)
    {
        try
        {
            Debug.Log("[Cafe] Received submit: " + playerText);

            if (_currentNpc == null)
                throw new Exception("현재 선택된 NPC가 없습니다.");

            var npcId = _currentNpc.npcId;

            bool useGpt = IsMainNpc(npcId) || IsDynamicQuestTarget(npcId);
            
            MarkTalked(npcId);

            if (useGpt)
            {
                Debug.Log("[Cafe] GPT path. Showing 'thinking' placeholder...");
                // thinkingText 보여주기
                var thinkingText = $"{_currentNpc.displayName}은(는) 답변을 생각 중인 것 같습니다...";
                dialoguePanel.ShowNpcLine(thinkingText);

                var expectedNpcId = npcId;

                var resp = await _dialogue.TalkAsync(_currentNpc, playerText, forceGpt: true);

                if (_currentNpc != null && string.Equals(_currentNpc.npcId, expectedNpcId, StringComparison.OrdinalIgnoreCase))
                    dialoguePanel.ShowNpcLine(resp.reply);
            }
            else
            {
                dialoguePanel.ShowNpcLine(GetLocalNpcReply(_currentNpc.npcId, _currentNpc.displayName));
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            dialoguePanel.ShowNpcLine("지금은 대화가 원활하지 않습니다. 잠시 후 다시 시도해 주세요.");
        }
    }

    private static bool IsMainNpc(string npcId)
    {
        return string.Equals(npcId, "yoonseo", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetIntroLine(string npcId, string name)
    {
        if (string.Equals(npcId, "yoonseo", StringComparison.OrdinalIgnoreCase))
            return $"{name}: 안녕하세요. 오늘 일정부터 정리할까요?";

        if (string.Equals(npcId, "harin", StringComparison.OrdinalIgnoreCase))
            return $"{name}: 잠깐만요. 지금 좀 바빠서… 짧게만 말씀해 주세요.";

        if (string.Equals(npcId, "sea", StringComparison.OrdinalIgnoreCase))
            return $"{name}: 어, 안녕하세요. 오늘은 분위기 괜찮네요.";

        return $"{name}: …";
    }

    private static string GetLocalNpcReply(string npcId, string name)
    {
        if (string.Equals(npcId, "harin", StringComparison.OrdinalIgnoreCase))
            return $"{name}: 오늘은 여기까지요. 다음에 다시 얘기해요.";

        if (string.Equals(npcId, "sea", StringComparison.OrdinalIgnoreCase))
            return $"{name}: 응, 좋아요. 또 봐요.";

        return "…";
    }

    private void MarkTalked(string npcId)
    {
        if (App.I == null) return;
        if (string.IsNullOrWhiteSpace(npcId)) return;

        // App.I.Npcs에 상태가 없으면 생성
        if (!App.I.Npcs.TryGetValue(npcId, out var s))
        {
            s = new NpcState { npcId = npcId, affinity = 0 };
            App.I.Npcs[npcId] = s;
        }

        // flag 세팅: talked_<npcid> = 1
        s.UpsertFlag($"talked_{npcId}", "1");

        Debug.Log($"[Quest] MarkTalked npcId={npcId} qmNull={QuestManager.I == null}");
        QuestManager.I?.Raise(new TalkEvent(npcId, "cafe"));

        // 즉시 저장(안전)
        App.I.Save();
    }

    private static bool IsDynamicQuestTarget(string npcId)
    {
        if (App.I == null) return false;

        bool started = App.I.GetGlobalBool("quest.dynamic_generated.started", false);
        bool done = App.I.GetGlobalBool("quest.dynamic_generated.done", false);
        if (!started || done) return false;

        var target = App.I.GetGlobalFlag("quest.dynamic_generated.targetNpcId");
        if (string.IsNullOrWhiteSpace(target)) return false;

        return string.Equals(target.Trim(), npcId, StringComparison.OrdinalIgnoreCase);
    }
}