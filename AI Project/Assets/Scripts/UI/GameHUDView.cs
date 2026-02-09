using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project.Core;

namespace Project.UI
{
    /// <summary>
    /// UGUI HUD 표시 + GameLoopManager 연결.
    /// - Day / TimeBlock 표시
    /// - 현재 시간대 핵심 행동 사용 여부 표시
    /// - 오늘 남은 핵심 행동 기회 표시
    /// - 이벤트 진행 중(Event Lock)에는 시간대 진행 잠금
    /// - 핵심 행동 소비 버튼은 "디버그용" (실제 게임에선 이벤트 종료 훅에서 ConsumeMajorAction 호출)
    /// </summary>
    public sealed class GameHUDView : MonoBehaviour
    {
        [Header("UI Refs")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI timeBlockText;
        [SerializeField] private TextMeshProUGUI actionGateText;
        [SerializeField] private TextMeshProUGUI remainingText;

        [SerializeField] private Button advanceTimeButton;

        [Header("Debug Button (Optional)")]
        [SerializeField] private Button consumeMajorActionDebugButton;
        [SerializeField] private bool showDebugButton = true;

        [Header("Options")]
        [SerializeField] private bool persistAcrossScenes = true;

        private GameLoopManager _loop;
        private static GameHUDView s_instance;

        private void Awake()
        {
            if (!persistAcrossScenes)
                return;

            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            StartCoroutine(BindRoutine());
        }

        private void OnDisable()
        {
            Unbind();
        }

        private IEnumerator BindRoutine()
        {
            const float timeout = 5f;
            float t = 0f;

            while (GameLoopManager.Instance == null && t < timeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            _loop = GameLoopManager.Instance;
            if (_loop == null)
            {
                Debug.LogError("[GameHUDView] GameLoopManager.Instance not found.");
                yield break;
            }

            Bind(_loop);
            RefreshAll();
        }

        private void Bind(GameLoopManager loop)
        {
            Unbind();

            _loop = loop;
            _loop.DayChanged += OnDayChanged;
            _loop.TimeBlockChanged += OnTimeBlockChanged;
            _loop.MajorActionUsedChanged += OnMajorActionUsedChanged;
            _loop.EventLockChanged += OnEventLockChanged;
            _loop.ForcedReturnHome += OnForcedReturnHome;

            if (advanceTimeButton != null)
                advanceTimeButton.onClick.AddListener(OnClickAdvanceTime);

            if (consumeMajorActionDebugButton != null)
                consumeMajorActionDebugButton.onClick.AddListener(OnClickConsumeMajorActionDebug);
        }

        private void Unbind()
        {
            if (_loop != null)
            {
                _loop.DayChanged -= OnDayChanged;
                _loop.TimeBlockChanged -= OnTimeBlockChanged;
                _loop.MajorActionUsedChanged -= OnMajorActionUsedChanged;
                _loop.EventLockChanged -= OnEventLockChanged;
                _loop.ForcedReturnHome -= OnForcedReturnHome;
                _loop = null;
            }

            if (advanceTimeButton != null)
                advanceTimeButton.onClick.RemoveListener(OnClickAdvanceTime);

            if (consumeMajorActionDebugButton != null)
                consumeMajorActionDebugButton.onClick.RemoveListener(OnClickConsumeMajorActionDebug);
        }

        private void RefreshAll()
        {
            if (_loop == null) return;

            SetDayText(_loop.Day, _loop.EndDay);
            SetTimeBlockText(_loop.CurrentTimeBlock);
            SetActionGateText(_loop.HasUsedMajorActionInCurrentBlock);
            SetRemainingText(_loop.RemainingMajorActionChances);

            ApplyEventLock(_loop.IsEventLocked);
            ApplyDebugButton();
        }

        private void ApplyEventLock(bool locked)
        {
            if (advanceTimeButton != null)
                advanceTimeButton.interactable = !locked;
        }

        private void ApplyDebugButton()
        {
            if (consumeMajorActionDebugButton == null)
                return;

            consumeMajorActionDebugButton.gameObject.SetActive(showDebugButton);
            consumeMajorActionDebugButton.interactable =
                showDebugButton && _loop != null && _loop.CanConsumeMajorAction();
        }

        // --- UI setters ---
        private void SetDayText(int day, int endDay)
        {
            if (dayText == null) return;
            dayText.text = $"Day {day} / {endDay}";
        }

        private void SetTimeBlockText(TimeBlock block)
        {
            if (timeBlockText == null) return;

            timeBlockText.text = block switch
            {
                TimeBlock.Morning => "아침",
                TimeBlock.Day => "낮",
                TimeBlock.Evening => "저녁",
                TimeBlock.Night => "밤",
                _ => block.ToString()
            };
        }

        private void SetActionGateText(bool used)
        {
            if (actionGateText == null) return;
            actionGateText.text = used ? "핵심 행동: 사용 완료" : "핵심 행동: 미사용";
        }

        private void SetRemainingText(int remaining)
        {
            if (remainingText == null) return;
            remainingText.text = $"남은 기회: {remaining}";
        }

        // --- GameLoop events ---
        private void OnDayChanged(int day)
        {
            if (_loop == null) return;
            SetDayText(day, _loop.EndDay);
        }

        private void OnTimeBlockChanged(TimeBlock block)
        {
            // 시간대가 바뀌면: 표시 3종 갱신
            SetTimeBlockText(block);
            SetActionGateText(_loop != null && _loop.HasUsedMajorActionInCurrentBlock);
            SetRemainingText(_loop != null ? _loop.RemainingMajorActionChances : 0);

            ApplyDebugButton();
        }

        private void OnMajorActionUsedChanged(bool used)
        {
            SetActionGateText(used);
            SetRemainingText(_loop != null ? _loop.RemainingMajorActionChances : 0);

            ApplyDebugButton();
        }

        private void OnEventLockChanged(bool locked)
        {
            ApplyEventLock(locked);
        }

        private void OnForcedReturnHome()
        {
            // 나중에 여기서 귀가 연출(페이드/사운드/짧은 멘트) 추가
        }

        // --- Button handlers ---
        private void OnClickAdvanceTime()
        {
            if (_loop == null) return;

            // 이벤트 진행 중이면 false(버튼이 비활성이라 보통 안 눌림)
            _loop.AdvanceTimeBlock();
        }

        private void OnClickConsumeMajorActionDebug()
        {
            if (_loop == null) return;

            // 실제 게임에서는 "이벤트 종료 지점"에서 ConsumeMajorAction()을 호출합니다.
            _loop.ConsumeMajorAction();
        }
    }
}
