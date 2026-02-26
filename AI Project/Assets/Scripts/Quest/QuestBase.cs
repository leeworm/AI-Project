using System;

public interface IQuest
{
    string Id { get; }
    string Title { get; }

    void Initialize(IQuestStore store);
    void Handle(IQuestEvent e);

    bool IsStarted { get; }
    bool IsCompleted { get; }
}

public abstract class QuestBase : IQuest
{
    protected IQuestStore Store;

    public abstract string Id { get; }
    public abstract string Title { get; }

    protected string KStarted => $"quest.{Id}.started";
    protected string KDone => $"quest.{Id}.done";

    public bool IsStarted => Store != null && Store.GetBool(KStarted);
    public bool IsCompleted => Store != null && Store.GetBool(KDone);

    public void Initialize(IQuestStore store)
    {
        Store = store ?? throw new ArgumentNullException(nameof(store));

        // 기본 키 초기화(없으면 0)
        if (Store.Get(KStarted) == null) Store.SetBool(KStarted, false);
        if (Store.Get(KDone) == null) Store.SetBool(KDone, false);

        OnInitialize();
    }

    public void Handle(IQuestEvent e)
    {
        if (Store == null) return;
        OnHandle(e);
    }

    protected void StartQuest()
    {
        if (!IsStarted) Store.SetBool(KStarted, true);
    }

    protected void CompleteQuest()
    {
        if (!IsCompleted) Store.SetBool(KDone, true);
    }

    protected virtual void OnInitialize() { }
    protected abstract void OnHandle(IQuestEvent e);
}