using Project.Core;
using System;

public sealed class Quest_DynamicGenerated : QuestBase
{
    public override string Id => "dynamic_generated";

    public override string Title
    {
        get
        {
            var t = Store?.Get($"quest.{Id}.title");
            return string.IsNullOrWhiteSpace(t) ? "동적 퀘스트" : t;
        }
    }

    public string Objective => Store?.Get($"quest.{Id}.objective") ?? "";
    public string TargetNpcId => (Store?.Get($"quest.{Id}.targetNpcId") ?? "").Trim();

    protected override void OnInitialize()
    {
        if (Store.Get($"quest.{Id}.title") == null) Store.Set($"quest.{Id}.title", "");
        if (Store.Get($"quest.{Id}.objective") == null) Store.Set($"quest.{Id}.objective", "");
        if (Store.Get($"quest.{Id}.targetNpcId") == null) Store.Set($"quest.{Id}.targetNpcId", "");
    }

    protected override void OnHandle(IQuestEvent e)
    {
        // started가 true일 때만 동작
        if (!IsStarted || IsCompleted) return;

        if (e is TalkEvent te)
        {
            if (!string.IsNullOrEmpty(TargetNpcId) &&
                string.Equals(te.NpcId, TargetNpcId, StringComparison.OrdinalIgnoreCase))
            {
                CompleteQuest();
            }
        }
    }
}