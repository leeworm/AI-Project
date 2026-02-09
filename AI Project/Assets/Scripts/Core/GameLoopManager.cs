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
            InitializeNewGame();
        }

        public void InitializeNewGame()
        {
            Day = Mathf.Clamp(startDay, 1, endDay);
            EnterTimeBlock(TimeBlock.Morning);
            SetEventLock(false);
            RaiseAll();
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
        }

        private void RaiseAll()
        {
            DayChanged?.Invoke(Day);
            TimeBlockChanged?.Invoke(CurrentTimeBlock);
            MajorActionUsedChanged?.Invoke(HasUsedMajorActionInCurrentBlock);
            EventLockChanged?.Invoke(IsEventLocked);
        }
    }
}
