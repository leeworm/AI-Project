using System;
using UnityEngine;

namespace Project.Core
{
    public enum TimeBlock
    {
        Morning,
        Day,
        Evening,
        Night
    }

    /// <summary>
    /// 하루 루프 규칙(아침→낮→저녁→밤).
    /// - 시간대는 언제든 건너뛸 수 있음(스킵 허용)
    /// - 각 시간대(아침/낮/저녁)에서 핵심 행동은 최대 1회
    /// - 핵심 행동은 "이벤트 종료 시점"에 소모 트리거로 사용
    /// - 핵심 행동 소모가 시간대를 자동으로 넘기지 않음
    /// - 이벤트 진행 중에는 시간대 전환 금지(중간 스킵 방지)
    /// </summary>
    public sealed class GameLoopManager : MonoBehaviour
    {
        public static GameLoopManager Instance { get; private set; }

        public event Action<int> DayChanged;
        public event Action<TimeBlock> TimeBlockChanged;
        public event Action<bool> MajorActionUsedChanged;
        public event Action ForcedReturnHome;

        public event Action<bool> EventLockChanged;

        [Header("Config")]
        [SerializeField] private int startDay = 1;
        [SerializeField] private int endDay = 30;

        public int Day { get; private set; }
        public int EndDay => endDay;
        public TimeBlock CurrentTimeBlock { get; private set; }

        // 현재 시간대(아침/낮/저녁)에서 핵심 행동을 이미 사용했는지
        public bool HasUsedMajorActionInCurrentBlock { get; private set; }

        // 이벤트(대화/미니게임/퀘스트 씬/모달 등) 진행 중 락
        public bool IsEventLocked { get; private set; }

        public int RemainingMajorActionChances
        {
            get
            {
                int remaining = CurrentTimeBlock switch
                {
                    TimeBlock.Morning => 3,
                    TimeBlock.Day => 2,
                    TimeBlock.Evening => 1,
                    TimeBlock.Night => 0,
                    _ => 0
                };

                if (CurrentTimeBlock != TimeBlock.Night && HasUsedMajorActionInCurrentBlock)
                    remaining -= 1;

                return Mathf.Max(0, remaining);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (App.I != null && App.I.World != null)
            {
                InitializeFromAppWorld();
            }
            else
            {
                InitializeNewGame();
            }
        }

        public void InitializeNewGame()
        {
            Day = Mathf.Clamp(startDay, 1, endDay);
            EnterTimeBlock(TimeBlock.Morning);
            SetEventLock(false);
            RaiseAll();
        }

        private void InitializeFromAppWorld()
        {
            // 최소한 day는 App.World.day 사용
            Day = Mathf.Clamp(App.I.World.day, 1, endDay);

            EnterTimeBlock(TokenToBlock(App.I.World.timeSlot));

            // 로드 직후엔 기본적으로 락 해제
            SetEventLock(false);

            // 미러 동기화(정규화)
            SyncToAppWorld();

            RaiseAll();

            if (App.I.World != null)
                App.I.World.timeSlot = BlockToToken(CurrentTimeBlock);
        }

        private static string BlockToToken(TimeBlock block)
        {
            return block switch
            {
                TimeBlock.Morning => "morning",
                TimeBlock.Day => "day",
                TimeBlock.Evening => "evening",
                TimeBlock.Night => "night",
                _ => "morning"
            };
        }

        private static TimeBlock TokenToBlock(string tokenOrLegacy)
        {
            if (string.IsNullOrWhiteSpace(tokenOrLegacy))
                return TimeBlock.Morning;

            // 공백 제거 + 소문자 정규화
            string v = tokenOrLegacy.Trim().ToLowerInvariant();

            // 1) 정상 토큰
            switch (v)
            {
                case "morning": return TimeBlock.Morning;
                case "day": return TimeBlock.Day;
                case "evening": return TimeBlock.Evening;
                case "night": return TimeBlock.Night;
            }

            // 2) 레거시/한글 저장값 호환(기존 세이브 살리기)
            // save.json에 "낮" 같은 값이 이미 들어가 있으니 꼭 필요합니다.
            switch (tokenOrLegacy.Trim())
            {
                case "아침": return TimeBlock.Morning;
                case "낮": return TimeBlock.Day;
                case "저녁": return TimeBlock.Evening;
                case "밤": return TimeBlock.Night;
            }

            // 3) 혹시 예전 영어 표현이 들어갔을 가능성까지(선택)
            switch (v)
            {
                case "noon":
                case "afternoon":
                    return TimeBlock.Day;
            }

            return TimeBlock.Morning;
        }

        private void SyncToAppWorld()
        {
            if (App.I == null || App.I.World == null) return;

            App.I.World.day = Day;
            App.I.World.timeSlot = BlockToToken(CurrentTimeBlock);
        }

        /// <summary>
        /// 이벤트 시작 시 호출: 시간대 전환 잠금
        /// </summary>
        public void BeginEvent()
        {
            SetEventLock(true);
        }

        /// <summary>
        /// 이벤트 종료 시 호출: 시간대 전환 잠금 해제
        /// </summary>
        public void EndEvent()
        {
            SetEventLock(false);
        }

        private void SetEventLock(bool locked)
        {
            if (IsEventLocked == locked)
                return;

            IsEventLocked = locked;
            EventLockChanged?.Invoke(IsEventLocked);
        }

        /// <summary>
        /// 핵심 행동을 "소모 처리"할 수 있는가?
        /// - 아침/낮/저녁에서만 가능
        /// - 각 시간대당 1회
        /// </summary>
        public bool CanConsumeMajorAction()
        {
            if (CurrentTimeBlock == TimeBlock.Night)
                return false;

            return !HasUsedMajorActionInCurrentBlock;
        }

        /// <summary>
        /// 이벤트 종료 시점에 호출해서 핵심 행동을 소모 처리합니다.
        /// (시간대는 자동으로 넘어가지 않습니다)
        /// </summary>
        public bool ConsumeMajorAction()
        {
            if (!CanConsumeMajorAction())
                return false;

            HasUsedMajorActionInCurrentBlock = true;
            MajorActionUsedChanged?.Invoke(true);
            return true;
        }

        /// <summary>
        /// 시간대 진행은 스킵 포함해서 언제든 가능.
        /// 단, 이벤트 진행 중에는 금지.
        /// </summary>
        public bool AdvanceTimeBlock()
        {
            if (IsEventLocked)
                return false;

            switch (CurrentTimeBlock)
            {
                case TimeBlock.Morning:
                    EnterTimeBlock(TimeBlock.Day);
                    return true;

                case TimeBlock.Day:
                    EnterTimeBlock(TimeBlock.Evening);
                    return true;

                case TimeBlock.Evening:
                    EnterTimeBlock(TimeBlock.Night);
                    return true;

                case TimeBlock.Night:
                    FinishNight();
                    return true;
            }

            return false;
        }

        public void FinishNight()
        {
            if (IsEventLocked)
                return;

            ForcedReturnHome?.Invoke();

            if (Day < endDay)
            {
                Day++;
                DayChanged?.Invoke(Day);
            }

            EnterTimeBlock(TimeBlock.Morning);
        }

        private void EnterTimeBlock(TimeBlock block)
        {
            CurrentTimeBlock = block;
            TimeBlockChanged?.Invoke(CurrentTimeBlock);

            // 새 시간대 시작 시 해당 시간대 핵심 행동은 "미사용"으로 초기화
            HasUsedMajorActionInCurrentBlock = false;
            MajorActionUsedChanged?.Invoke(false);

            SyncToAppWorld();

            if (App.I != null && App.I.World != null)
            {
                App.I.World.day = Day;
                App.I.World.timeSlot = BlockToToken(CurrentTimeBlock);
            }
        }

        private void RaiseAll()
        {
            DayChanged?.Invoke(Day);
            TimeBlockChanged?.Invoke(CurrentTimeBlock);
            MajorActionUsedChanged?.Invoke(HasUsedMajorActionInCurrentBlock);
            EventLockChanged?.Invoke(IsEventLocked);
        }
        public void ApplyFromAppWorld()
        {
            if (App.I == null || App.I.World == null)
            {
                InitializeNewGame();
                return;
            }

            Day = Mathf.Clamp(App.I.World.day, 1, endDay);
            EnterTimeBlock(TokenToBlock(App.I.World.timeSlot)); // 기존에 만들어둔 TokenToBlock 사용
            SetEventLock(false);
            RaiseAll();

            // 저장 토큰 정규화까지 확실히
            if (App.I.World != null)
                App.I.World.timeSlot = BlockToToken(CurrentTimeBlock); // 기존 BlockToToken 사용
        }
    }
}
