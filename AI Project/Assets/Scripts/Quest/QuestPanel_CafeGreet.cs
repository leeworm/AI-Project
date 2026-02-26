using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanel_CafeGreet : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GameObject checkYoonseo;
    [SerializeField] private GameObject checkHarin;
    [SerializeField] private GameObject checkSea;
    [SerializeField] private GameObject bodyRoot;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleText;

    private Quest_CafeGreet _quest;
    private bool _expanded;
    private bool _wasCompleted;

    private QuestPanelController _parentPanelController;

    private void Awake()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(Toggle);

        SetExpanded(false);
    }

    private void OnEnable()
    {
        if (_parentPanelController == null)
            _parentPanelController = GetComponentInParent<QuestPanelController>();

        if (QuestManager.I != null)
            QuestManager.I.Changed += OnQuestChanged;

        _quest = QuestManager.I?.FindQuest("cafe_greet") as Quest_CafeGreet;

        Bind();
        Refresh();
    }

    private void OnDisable()
    {
        if (QuestManager.I != null)
            QuestManager.I.Changed -= OnQuestChanged;
    }

    private void OnQuestChanged()
    {
        Bind();
        Refresh();
    }

    private void Bind()
    {
        if (QuestManager.I == null) return;

        // QuestManager가 아직 초기화 안 됐을 수 있으니 강제
        QuestManager.I.InitializeIfNeeded();
        _quest = QuestManager.I.Get<Quest_CafeGreet>();
    }

    public void Refresh()
    {
        if (_quest == null)
            return;
        
        int done = 0;
        if (_quest.TalkedYoonseo) done++;
        if (_quest.TalkedHarin) done++;
        if (_quest.TalkedSea) done++;

        bool completed = _quest.IsCompleted || (done >= 3);

        if (!completed)
        {
            if (titleText != null)
                titleText.text = $"퀘스트:\n{_quest.Title} {done}/3";
        }
        else
        {
            if (titleText != null)
                titleText.text = $"퀘스트: {_quest.Title} 완료!";
        }

        if (checkYoonseo != null) checkYoonseo.SetActive(_quest.TalkedYoonseo);
        if (checkHarin != null) checkHarin.SetActive(_quest.TalkedHarin);
        if (checkSea != null) checkSea.SetActive(_quest.TalkedSea);

        // 완료되면 자동 접힘(거슬리지 않게) + 표시 변경
        if (_quest.IsCompleted && !_wasCompleted)
        {
            // 1) 로컬 바디 숨김(토글 텍스트 갱신)
            SetExpanded(false);

            // 2) 부모 패널(전체 UI)도 확장 상태라면 접기
            if (_parentPanelController != null && _parentPanelController.IsExpanded)
            {
                // Toggle은 공개 API로 상태를 뒤집으므로 현재 확장 상태일 때만 호출
                _parentPanelController.Toggle();
            }

            // 제목 강조(완료 표시)
            if (titleText != null)
                titleText.text = $"퀘스트: \n{_quest.Title} 완료!";
        }

        _wasCompleted = _quest.IsCompleted;
    }
    private void Toggle()
    {
        SetExpanded(!_expanded);
    }

    private void SetExpanded(bool expanded)
    {
        _expanded = expanded;

        if (bodyRoot != null)
            bodyRoot.SetActive(expanded);

        if (toggleText != null)
            toggleText.text = expanded ? "▲" : "▼";
    }
}