using Project.Core;
using System;

public sealed class Quest_CafeGreet : QuestBase, IAdvanceTimeOnComplete
{
    public override string Id => "cafe_greet";
    public override string Title => "동료들과 인사하기";

    private const string KY = "quest.cafe_greet.talked_yoonseo";
    private const string KH = "quest.cafe_greet.talked_harin";
    private const string KS = "quest.cafe_greet.talked_sea";

    protected override void OnInitialize()
    {
        if (Store.Get(KY) == null) Store.SetBool(KY, false);
        if (Store.Get(KH) == null) Store.SetBool(KH, false);
        if (Store.Get(KS) == null) Store.SetBool(KS, false);
    }

    protected override void OnHandle(IQuestEvent e)
    {
        if (IsCompleted) return;

        if (e is TalkEvent te)
        {
            // 시작 조건: 카페에서 첫 대화 발생(가장 단순/안전)
            if (!IsStarted && te.LocationId == "cafe")
                StartQuest();

            // 진행 조건: NPC별 대화 1회
            if (te.LocationId == "cafe")
            {
                if (Eq(te.NpcId, "yoonseo")) Store.SetBool(KY, true);
                if (Eq(te.NpcId, "harin")) Store.SetBool(KH, true);
                if (Eq(te.NpcId, "sea")) Store.SetBool(KS, true);
            }

            // 완료 조건
            bool done = Store.GetBool(KY) && Store.GetBool(KH) && Store.GetBool(KS);
            if (done) CompleteQuest();
        }
    }

    public bool TalkedYoonseo => Store.GetBool(KY);
    public bool TalkedHarin => Store.GetBool(KH);
    public bool TalkedSea => Store.GetBool(KS);

    private static bool Eq(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}