using Project.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager I { get; private set; }

    public event Action Changed;

    private readonly List<IQuest> _quests = new();
    private IQuestStore _store;
    private bool _initialized;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }
    public void NotifyChanged()
    {
        Changed?.Invoke();
    }
    public void InitializeIfNeeded()
    {
        if (App.I == null) return;

        // 1) 항상 최신 __global로 store 재바인딩
        _store = new QuestStore_AppGlobal(App.I.GetOrCreateGlobal());

        // 2) 최초 1회만 퀘스트 등록
        if (!_initialized)
        {
            // 여기서 퀘스트 등록
            _quests.Add(new Quest_CafeGreet());
            _quests.Add(new Quest_DynamicGenerated());

            _initialized = true;
        }

        // 3) 매번 store를 다시 주입(Load로 Npcs가 갈아끼워져도 안전)
        foreach (var q in _quests)
            q.Initialize(_store);

        // 초기화 직후 한 번 저장해두면 __global이 save.json에 반드시 저장됨
        App.I.Save();
    }

    public void Raise(IQuestEvent e)
    {
        InitializeIfNeeded();
        if (!_initialized) return;

        // 1) cafe_greet 완료 상태 스냅샷
        IQuest cafe = null;
        bool wasCompleted = false;

        foreach (var q in _quests)
        {
            if (q != null && q.Id == "cafe_greet")
            {
                cafe = q;
                wasCompleted = q.IsCompleted;
                break;
            }
        }

        // 2) 이벤트 처리
        foreach (var q in _quests)
            q.Handle(e);

        // 3) 완료 "전환 순간"에만 시간 진행 1회
        if (cafe != null && !wasCompleted && cafe.IsCompleted)
        {
            var loop = GameLoopManager.Instance;
            if (loop != null)
            {
                // 락 때문에 실패할 수 있으니: 1) 시도 -> 2) 락이면 EndEvent 후 재시도(1회)
                bool ok = loop.AdvanceTimeBlock();
                if (!ok && loop.IsEventLocked)
                {
                    loop.EndEvent();
                    ok = loop.AdvanceTimeBlock();
                }

                Debug.Log($"[QuestManager] cafe_greet complete -> advance ok={ok}, block={loop.CurrentTimeBlock}, locked={loop.IsEventLocked}");
            }
            else
            {
                Debug.LogWarning("[QuestManager] GameLoopManager.Instance is null.");
            }
        }

        // 4) 저장/갱신
        App.I.Save();
        NotifyChanged();

        var quest = FindQuest("cafe_greet");
        Debug.Log($"[Quest] cafe_greet done={quest?.IsCompleted}");
    }

    public IReadOnlyList<IQuest> GetAll()
    {
        InitializeIfNeeded();
        return _quests;
    }

    public T Get<T>() where T : class, IQuest
    {
        InitializeIfNeeded();
        foreach (var q in _quests)
            if (q is T t) return t;
        return null;
    }

    public IQuest FindQuest(string id)
    {
        InitializeIfNeeded();
        foreach (var q in _quests)
            if (q != null && q.Id == id)
                return q;
        return null;
    }
}