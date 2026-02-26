using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DialoguePanelController : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text npcText;
    [SerializeField] private GameObject npcTextRoot;   // NPC 텍스트 묶음(없으면 npcText.gameObject로 대체)
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private GameObject inputRoot;      // 입력 UI 묶음(없으면 inputField.gameObject로 대체)
    [SerializeField] private GameObject nextRoot;       // "다음" 버튼 묶음
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    [Header("Optional")]
    [SerializeField] private bool useTypewriter = false;
    [SerializeField] private float typewriterCps = 40f; // chars per second

    [Header("SFX")]
    [SerializeField] private AudioClip advanceClip;

    private enum Phase { WaitingNpc, WaitingAdvance, WaitingPlayerInput, Sending }
    private Phase _phase = Phase.WaitingNpc;

    private string _fullNpcLine;
    private int _typeIndex;
    private float _typeAcc;

    private bool _sendingGuard;

    private int _lastAdvanceFrame = -1;

    private void Awake()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnClickNext);
        
            // Enter/Space가 Submit로 버튼 클릭되는 걸 최대한 방지
            var nav = nextButton.navigation;
            nav.mode = Navigation.Mode.None;
            nextButton.navigation = nav;
        }

        if (inputField != null)
            inputField.onSubmit.AddListener(_ => { /* TMP 기본 submit은 멀티라인에서 잘 안 쓰니 보조 */ });

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseDialogue);

            var nav = closeButton.navigation;
            nav.mode = Navigation.Mode.None;
            closeButton.navigation = nav;
        }

        if (inputField != null)
        {
            // Enter(Submit) 시 호출됨
            inputField.onSubmit.AddListener(_ => SendCurrentInput());
        }

        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(OnInputValueChanged);
        }

        SetPhase(Phase.WaitingNpc);
    }

    private void Update()
    {
        // 1) NPC 출력 타이핑(선택)
        if (useTypewriter && _phase == Phase.WaitingNpc)
            TickTypewriter();

        // 2) 진행키 처리(Enter/Space): 입력창 포커스면 무시
        if ((_phase == Phase.WaitingNpc || _phase == Phase.WaitingAdvance) &&
            !IsTypingInInputField() &&
            IsAdvancePressedThisFrame())
        {
            TryAdvance(playSfx: true);
        }

        // 2) 진행키 처리(다음 단계)
        // if (_phase == Phase.WaitingAdvance && IsAdvancePressed())
        // {
        //    OnClickNext();
        // }

        // 3) 입력 중 Enter/Shift+Enter 처리
        // if (_phase == Phase.WaitingPlayerInput)
        // {
        //     HandleEnterToSend();
        // }
    }

    // ---- 외부에서 호출: NPC 라인 세팅 ----
    public void ShowNpcLine(string line)
    {
        _fullNpcLine = line ?? "";

        // 안전장치: 패널 자체가 꺼져 있으면 켬
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // NPC 텍스트 루트/텍스트 강제로 켬
        if (npcTextRoot != null && !npcTextRoot.activeSelf)
            npcTextRoot.SetActive(true);

        if (npcText != null && !npcText.gameObject.activeSelf)
            npcText.gameObject.SetActive(true);

        // 텍스트 먼저 직접 세팅(타이핑 연출이든 뭐든 일단 보이게)
        if (npcText != null)
            npcText.text = _fullNpcLine;

        if (useTypewriter)
        {
            _typeIndex = 0;
            _typeAcc = 0f;

            npcText.maxVisibleCharacters = 0;

            // 레이아웃 안정화(선택)
            npcText.ForceMeshUpdate(true, true);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(npcText.rectTransform);
            Canvas.ForceUpdateCanvases();

            SetPhase(Phase.WaitingNpc);
        }
        else
        {
            npcText.maxVisibleCharacters = int.MaxValue;
            SetPhase(Phase.WaitingAdvance);
        }
        Debug.Log($"[UI] ShowNpcLine set. panelActive={gameObject.activeSelf}, textRootActive={(npcTextRoot != null ? npcTextRoot.activeSelf : false)}, textActive={(npcText != null ? npcText.gameObject.activeSelf : false)}, textLen={(npcText != null ? npcText.text.Length : 0)}");
    }

    // ---- 다음 클릭 ----
    private void OnClickNext()
    {
        Debug.Log($"OnClickNext phase={_phase}");
        TryAdvance(playSfx: true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_phase == Phase.WaitingNpc || _phase == Phase.WaitingAdvance)
            TryAdvance(playSfx: true);
    }

    // ---- Enter=전송, Shift+Enter=줄바꿈 ----
    private void HandleEnterToSend()
    {
        // TMP 멀티라인에서는 Enter가 줄바꿈으로 들어가려 하기 때문에,
        // Update에서 KeyDown을 가로채는 방식이 가장 확실합니다.
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
            return;

        // Shift가 눌려 있으면 줄바꿈 허용(기본 동작)
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (shift) return;

        // Enter만 누른 경우: 전송
        // 이벤트 시스템에 의해 Enter가 입력필드에 줄바꿈으로 들어가는 걸 막기 위해,
        // 다음 프레임에 줄바꿈이 추가되지 않도록 강제로 처리합니다.
        // (대부분의 경우 이 방식으로 충분합니다)
        SendCurrentInput();
    }

    private void SendCurrentInput()
    {
        Debug.Log("[Dialogue] SendCurrentInput called");
        if (_phase != Phase.WaitingPlayerInput) return;

        var text = inputField.text?.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        SetPhase(Phase.Sending);

        // 입력 UI 숨김
        SetInputVisible(false);

        // (선택) 입력창 비우기
        inputField.text = "";

        // 여기서 실제 대화 호출:
        // resp = await dialogueService.TalkAsync(npcDef, text);
        // ShowNpcLine(resp.reply);

        OnPlayerSubmit?.Invoke(text);
        Debug.Log("[Dialogue] OnPlayerSubmit invoked: " + text);
    }

    // ---- 타입라이터 처리 ----
    private void TickTypewriter()
    {
        if (_fullNpcLine == null) _fullNpcLine = "";

        if (_typeIndex >= _fullNpcLine.Length)
        {
            // 출력 완료 -> 다음 단계로
            SetPhase(Phase.WaitingAdvance);
            return;
        }

        _typeAcc += Time.unscaledDeltaTime * typewriterCps;
        int add = Mathf.FloorToInt(_typeAcc);
        if (add <= 0) return;

        _typeAcc -= add;
        _typeIndex = Mathf.Min(_typeIndex + add, _fullNpcLine.Length);
        npcText.maxVisibleCharacters = _typeIndex;
    }

    public void CloseDialogue()
    {
        // 패널 자체를 끄는 구조를 권장
        gameObject.SetActive(false);

        // 내부 상태 정리(다음에 열 때 깔끔하게)
        if (inputField != null) inputField.text = "";
        if (npcText != null) npcText.text = "";
    }


    // ---- UI Phase 관리 ----
    private void SetPhase(Phase phase)
    {
        _phase = phase;

        switch (_phase)
        {
            case Phase.WaitingNpc:
                ClearSelection();
                SetNpcTextVisible(true);
                SetNextEnabled(false);
                SetInputVisible(false);
                break;

            case Phase.WaitingAdvance:
                ClearSelection();
                SetNpcTextVisible(true);
                SetNextEnabled(true);
                SetInputVisible(false);
                break;

            case Phase.WaitingPlayerInput:
                SetNpcTextVisible(false);
                SetNextEnabled(false);
                SetInputVisible(true);
                break;

            case Phase.Sending:
                ClearSelection();
                SetNpcTextVisible(false);
                SetNextEnabled(false);
                SetInputVisible(false);
                break;
        }
    }
    private void SetNpcTextVisible(bool visible)
    {
        if (npcTextRoot != null)
            npcTextRoot.SetActive(visible);
        else if (npcText != null)
            npcText.gameObject.SetActive(visible);
    }

    private void SetInputVisible(bool visible)
    {
        if (inputRoot != null) inputRoot.SetActive(visible);
        else if (inputField != null) inputField.gameObject.SetActive(visible);

        if (visible)
            FocusInput();
    }

    private void SetNextEnabled(bool enabled)
    {
        if (nextButton != null)
            nextButton.interactable = enabled;

        // nextRoot가 있더라도 "보이기/숨기기"는 하지 않습니다.
        if (nextRoot != null && !nextRoot.activeSelf)
            nextRoot.SetActive(true);
    }

    private void FocusInput()
    {
        if (inputField == null) return;

        inputField.ActivateInputField();
        inputField.Select();

        // EventSystem 포커스 확실히
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
    }

    private void OnInputValueChanged(string value)
    {
        if (_phase != Phase.WaitingPlayerInput) return;
        if (_sendingGuard) return;
        if (string.IsNullOrEmpty(value)) return;

        // Enter가 들어오면 보통 끝에 \n 또는 \r\n 이 붙습니다.
        bool endsWithNewline = value.EndsWith("\n") || value.EndsWith("\r\n") || value.EndsWith("\r");
        if (!endsWithNewline) return;

        // Shift가 눌려 있으면 줄바꿈 허용
        bool shift = Keyboard.current != null &&
                     (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        if (shift)
            return;

        // Shift 없이 Enter -> 전송
        _sendingGuard = true;

        // 줄바꿈 제거
        inputField.text = value.TrimEnd('\r', '\n');

        // 캐럿을 끝으로(안 하면 가끔 커서가 이상해짐)
        inputField.caretPosition = inputField.text.Length;

        // 전송
        SendCurrentInput();

        _sendingGuard = false;
    }

    // ---- 외부 연결용 이벤트 ----
    public event Action<string> OnPlayerSubmit;

    private bool IsAdvancePressedThisFrame()
    {
        if (Keyboard.current == null) return false;

        bool enter =
            Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame;

        bool space = Keyboard.current.spaceKey.wasPressedThisFrame;

        return enter || space;
    }

    private static bool IsTypingInInputField()
    {
        if (EventSystem.current == null) return false;

        var go = EventSystem.current.currentSelectedGameObject;
        if (go == null) return false;

        return go.GetComponent<TMP_InputField>() != null;
    }

    private void TryAdvance(bool playSfx)
    {
        // 같은 프레임에 중복 호출(키 + Submit 등) 방지
        if (_lastAdvanceFrame == Time.frameCount) return;
        _lastAdvanceFrame = Time.frameCount;

        // 타입라이터 출력 중이면: 1회 입력 = 즉시 완성
        if (_phase == Phase.WaitingNpc)
        {
            if (!useTypewriter) return;

            if (playSfx) AudioHub.I?.PlayUIClick();

            npcText.maxVisibleCharacters = int.MaxValue;
            _typeIndex = npcText.text.Length;
            _typeAcc = 0f;

            SetPhase(Phase.WaitingAdvance);
            return;
        }

        // 다음 대기 상태면: 입력 단계로
        if (_phase == Phase.WaitingAdvance)
        {
            if (playSfx) AudioHub.I?.PlayUIClick();

            SetPhase(Phase.WaitingPlayerInput);
            FocusInput();
        }
    }

    private void ClearSelection()
    {
        if (EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
    }
}