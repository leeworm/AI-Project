using System;
using UnityEngine;
using UnityEngine.UI;

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

            MarkTalked(_currentNpc.npcId);

            // 윤서만 API, 나머지는 로컬 고정 응답
            if (IsMainNpc(_currentNpc.npcId))
            {
                Debug.Log("[Cafe] Main NPC selected. Showing 'thinking' placeholder...");

                // 즉시 UI에 생각 중 텍스트를 보여줌
                var thinkingText = $"{_currentNpc.displayName}은(는) 답변을 생각 중인 것 같습니다...";
                dialoguePanel.ShowNpcLine(thinkingText);

                // 현재 대화 상대 캡처(응답 도착 시 동일 상대인지 확인)
                var expectedNpcId = _currentNpc.npcId;

                Debug.Log("[Cafe] Calling API...");
                var resp = await _dialogue.TalkAsync(_currentNpc, playerText);
                Debug.Log("[Cafe] API returned. reply len=" + (resp?.reply?.Length ?? 0));

                // 사용자가 아직 같은 NPC와 대화 중일 때만 응답으로 갱신
                if (_currentNpc != null && string.Equals(_currentNpc.npcId, expectedNpcId, StringComparison.OrdinalIgnoreCase))
                {
                    dialoguePanel.ShowNpcLine(resp.reply);
                    Debug.Log("[Cafe] ShowNpcLine called with API reply.");
                }
                else
                {
                    Debug.Log("[Cafe] API reply ignored because current NPC changed.");
                }
            }
            else
            {
                // 로컬 1턴 응답(빠르게)
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
}