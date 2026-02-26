using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    [Header("New Game UI")]
    [SerializeField] private GameObject newGamePanel;
    [SerializeField] private Button slot1Button;
    [SerializeField] private Button slot2Button;
    [SerializeField] private Button slot3Button;
    [SerializeField] private Button cancelNewGameButton;

    [Header("Overwrite Confirm UI")]
    [SerializeField] private GameObject overwritePanel;
    [SerializeField] private TMP_Text overwriteText;
    [SerializeField] private Button overwriteYesButton;
    [SerializeField] private Button overwriteNoButton;


    [SerializeField] private Button deleteSlot1Button;
    [SerializeField] private Button deleteSlot2Button;
    [SerializeField] private Button deleteSlot3Button;

    private int _pendingSlot = 1;
    private int _pendingDeleteSlot = 1;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text hintText;

    [SerializeField] private int slot = 1;

    private void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnClickContinue);

        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnClickNewGame);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnClickOptions);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnClickQuit);

        if (slot1Button != null) slot1Button.onClick.AddListener(() => OnPickSlot(1));
        if (slot2Button != null) slot2Button.onClick.AddListener(() => OnPickSlot(2));
        if (slot3Button != null) slot3Button.onClick.AddListener(() => OnPickSlot(3));

        if (deleteSlot1Button != null) deleteSlot1Button.onClick.AddListener(() => OnClickDeleteSlot(1));
        if (deleteSlot2Button != null) deleteSlot2Button.onClick.AddListener(() => OnClickDeleteSlot(2));
        if (deleteSlot3Button != null) deleteSlot3Button.onClick.AddListener(() => OnClickDeleteSlot(3));

        if (overwriteYesButton != null) overwriteYesButton.onClick.AddListener(OnConfirmOverwriteYes);
        if (overwriteNoButton != null) overwriteNoButton.onClick.AddListener(OnConfirmOverwriteNo);

        if (newGamePanel != null) newGamePanel.SetActive(false);
        if (overwritePanel != null) overwritePanel.SetActive(false);

        if (cancelNewGameButton != null)
            cancelNewGameButton.onClick.AddListener(() =>
            {
                if (newGamePanel != null) newGamePanel.SetActive(false);
                if (overwritePanel != null) overwritePanel.SetActive(false);
            });


        Refresh();
    }

    private void Refresh()
    {
        // 세이브 존재 여부로 Continue 활성/비활성
        bool hasSave = HasSave(slot);

        if (continueButton != null)
            continueButton.interactable = hasSave;

        if (hintText != null)
            hintText.text = hasSave ? "" : "저장된 게임이 없습니다.";
    }

    private static bool HasSave(int s)
    {
        return App.I != null && App.I.HasSave(s);
    }
    private void RefreshSlotButtons()
    {
        if (App.I == null) return;

        SetSlotButtonText(slot1Button, 1);
        SetSlotButtonText(slot2Button, 2);
        SetSlotButtonText(slot3Button, 3);
    }

    private void SetSlotButtonText(Button b, int s)
    {
        if (b == null) return;

        var label = b.GetComponentInChildren<TMP_Text>();
        if (label == null) return;

        bool used = App.I.HasSave(s);
        label.text = used ? $"SLOT {s} (덮어씀)" : $"SLOT {s} (빈 슬롯)";
    }
    private void OnPickSlot(int pickedSlot)
    {
        Debug.Log($"[NewGame] PickSlot={pickedSlot}");

        _pendingSlot = pickedSlot;

        if (App.I == null)
        { Debug.LogError("[NewGame] App.I is null"); 
            return;
        }

        bool used = App.I.HasSave(pickedSlot);
        if (!used)
        {
            Debug.Log("[NewGame] Empty slot -> Start game");
            if (newGamePanel != null) newGamePanel.SetActive(false);
            SceneRouter.I.StartNewGameSlot(pickedSlot);
            return;
        }

        // 사용 중 슬롯이면 덮어쓰기 확인
        if (overwriteText != null)
            overwriteText.text = $"슬롯 {pickedSlot}을(를) 덮어쓸까요?";

        if (overwritePanel != null)
        {
            overwritePanel.SetActive(true);
            Debug.Log($"[NewGame] overwritePanel active={overwritePanel.activeSelf}");
        }
        else
        {
            Debug.LogError("[NewGame] overwritePanel is null (Inspector 연결 안 됨)");
        }
    }

    private void OnClickDeleteSlot(int s)
    {
        if (App.I == null) return;

        if (!App.I.HasSave(s))
            return; // 빈 슬롯이면 삭제할 것 없음

        _pendingDeleteSlot = s;

        if (overwriteText != null)
            overwriteText.text = $"슬롯 {s}을(를) 삭제할까요?";

        // Yes/No 버튼의 의미를 "삭제/취소"로 잠깐 바꿔도 되고,
        // 텍스트만 바꾸기 싫으면 그대로 둬도 됨(기능만 삭제로 처리)
        if (overwritePanel != null)
            overwritePanel.SetActive(true);

        // 이때 "현재 모드가 삭제인지 덮어쓰기인지" 구분 플래그가 필요
        _confirmMode = ConfirmMode.Delete;
    }

    private enum ConfirmMode { Overwrite, Delete }
    private ConfirmMode _confirmMode = ConfirmMode.Overwrite;

    private void OnConfirmOverwriteYes()
    {
        if (overwritePanel != null) overwritePanel.SetActive(false);

        if (_confirmMode == ConfirmMode.Delete)
        {
            App.I.DeleteSlot(_pendingDeleteSlot);
            RefreshSlotButtons();
            Refresh(); // Continue 활성화 갱신
            return;
        }

        // 덮어쓰기(새 게임 시작)
        if (newGamePanel != null) newGamePanel.SetActive(false);
        SceneRouter.I.StartNewGameSlot(_pendingSlot);
    }

    private void OnConfirmOverwriteNo()
    {
        if (overwritePanel != null) overwritePanel.SetActive(false);

    }

    private void OnClickContinue()
    {
        SceneRouter.I.Continue(slot);
    }

    private void OnClickNewGame()
    {
        // 기본 슬롯은 1
        int defaultSlot = slot;

        // 저장이 하나도 없으면 -> 바로 시작(슬롯1)
        bool anySave =
            (App.I != null) &&
            (App.I.HasSave(1) || App.I.HasSave(2) || App.I.HasSave(3));

        if (!anySave)
        {
            SceneRouter.I.StartNewGameSlot(defaultSlot);
            return;
        }

        // 저장이 있으면 -> 슬롯 선택 패널
        if (newGamePanel != null)
            newGamePanel.SetActive(true);

        RefreshSlotButtons();
    }


    private void OnClickOptions()
    {
        SceneRouter.I.Go(SceneId.Options);
    }

    private void OnClickQuit()
    {
#if UNITY_EDITOR
        Debug.Log("Quit (Editor)");
#else
        Application.Quit();
#endif
    }
}
