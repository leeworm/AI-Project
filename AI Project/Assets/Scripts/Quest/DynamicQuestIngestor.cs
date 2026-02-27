using System;
using UnityEngine;

[Serializable]
public class DynamicQuestPayload
{
    public string id;          // 데모용: "dynamic_generated" 고정 권장
    public string title;       // 표시용 제목
    public string objective;   // 목표 문장
    public string targetNpcId; // "yoonseo" | "harin" | "sea"
}

public static partial class DynamicQuestIngestor
{
    private const string BeginTag = "QUEST_JSON_START";
    private const string EndTag = "QUEST_JSON_END";

    // npcReply에서 JSON을 추출/저장하고, 화면/로그용으로는 JSON을 제거한 텍스트만 반환
    public static string IngestAndStrip(string npcReply)
    {
        if (string.IsNullOrEmpty(npcReply))
            return "";

        int b = npcReply.IndexOf(BeginTag, StringComparison.Ordinal);
        if (b < 0)
            return npcReply;

        int e = npcReply.IndexOf(EndTag, b + BeginTag.Length, StringComparison.Ordinal);
        if (e < 0)
            return npcReply;

        // 태그가 없으면: 코드펜스/마지막 JSON 블록을 찾아서 파싱 시도(데모용 폴백)
        if (b < 0)
        {
            TryApplyFromLooseJson(npcReply);
            return npcReply;
        }

        string json = npcReply.Substring(b + BeginTag.Length, e - (b + BeginTag.Length)).Trim();
        string clean = npcReply.Substring(0, b).TrimEnd();

        TryApply(json);

        return clean;
    }

    private static void TryApply(string json)
    {
        try
        {
            var payload = JsonUtility.FromJson<DynamicQuestPayload>(json);
            if (payload == null) return;

            string id = string.IsNullOrWhiteSpace(payload.id) ? "dynamic_generated" : payload.id.Trim();
            string title = payload.title?.Trim();
            string objective = payload.objective?.Trim();
            string target = payload.targetNpcId?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(title)) return;
            if (string.IsNullOrWhiteSpace(objective)) return;
            if (string.IsNullOrWhiteSpace(target)) return;

            // 최소 검증(데모용)
            if (target != "yoonseo" && target != "harin" && target != "sea")
                return;

            // QuestStore_AppGlobal(= __global flags)에 저장
            App.I.SetGlobalFlag($"quest.{id}.title", title);
            App.I.SetGlobalFlag($"quest.{id}.objective", objective);
            App.I.SetGlobalFlag($"quest.{id}.targetNpcId", target);

            App.I.SetGlobalBool($"quest.{id}.started", true);
            App.I.SetGlobalBool($"quest.{id}.done", false);

            App.I.Save();

            // 패널 갱신 트리거
            QuestManager.I?.NotifyChanged();
        }
        catch
        {
            // 데모 안정성: 실패하면 조용히 무시
        }
    }

    private static void TryApplyFromLooseJson(string text)
    {
        // ```json ... ``` 제거
        string t = text.Replace("```json", "```", StringComparison.OrdinalIgnoreCase);

        int fenceStart = t.LastIndexOf("```", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            int fenceEnd = t.IndexOf("```", fenceStart + 3, StringComparison.Ordinal);
            if (fenceEnd > fenceStart)
            {
                string inside = t.Substring(fenceStart + 3, fenceEnd - (fenceStart + 3)).Trim();
                TryApply(inside);
                return;
            }
        }

        // 마지막 { ... } 블록 시도
        int rb = t.LastIndexOf('}');
        int lb = t.LastIndexOf('{');
        if (lb >= 0 && rb > lb)
        {
            string json = t.Substring(lb, rb - lb + 1).Trim();
            TryApply(json);
        }
    }

    public static void TryApplyFromNote(string reply, string note)
    {
        if (App.I == null) return;

        // note가 없으면 reply에서라도 추출 시도
        string src = string.IsNullOrWhiteSpace(note) ? (reply ?? "") : note.Trim();
        if (string.IsNullOrWhiteSpace(src)) return;

        // 목표 NPC 추출(한글/영문 둘 다)
        string target = ExtractTargetNpcId(src);
        if (string.IsNullOrEmpty(target)) return;

        const string id = "dynamic_generated";

        // title/objective 최소 구성(데모용)
        string title = "대화로 받은 퀘스트";
        string objective = string.IsNullOrWhiteSpace(note) ? TrimTo(src, 60) : TrimTo(note, 60);

        App.I.SetGlobalFlag($"quest.{id}.title", title);
        App.I.SetGlobalFlag($"quest.{id}.objective", objective);
        App.I.SetGlobalFlag($"quest.{id}.targetNpcId", target);

        App.I.SetGlobalBool($"quest.{id}.started", true);
        App.I.SetGlobalBool($"quest.{id}.done", false);

        App.I.Save();
        QuestManager.I?.NotifyChanged();
    }

    private static string ExtractTargetNpcId(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;

        // 한글 이름/표현 대응
        if (s.Contains("윤서", StringComparison.OrdinalIgnoreCase) || s.Contains("yoonseo", StringComparison.OrdinalIgnoreCase))
            return "yoonseo";
        if (s.Contains("하린", StringComparison.OrdinalIgnoreCase) || s.Contains("harin", StringComparison.OrdinalIgnoreCase))
            return "harin";
        if (s.Contains("세아", StringComparison.OrdinalIgnoreCase) || s.Contains("sea", StringComparison.OrdinalIgnoreCase))
            return "sea";

        return null;
    }

    private static string TrimTo(string s, int max)
    {
        s = s.Replace("\r", " ").Replace("\n", " ").Trim();
        if (s.Length <= max) return s;
        return s.Substring(0, max).Trim();
    }
}