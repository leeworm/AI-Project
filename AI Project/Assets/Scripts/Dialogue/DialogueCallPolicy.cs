using System;

public static class DialogueCallPolicy
{
    public static bool ShouldCallGpt(string playerInput)
    {
        if (string.IsNullOrWhiteSpace(playerInput)) return false;

        string[] triggers =
        {
            "고백","사과","화해","결정","선택","퀘스트","의심","진실", "문제","도전","목적","목표",
            "왜","어떻게","도와","계획","비밀","중요","지금 당장", "어떤", "누구", "언제", "어디", "무엇", "어째서", "어떻게", "왜냐하면", "말해줘", "알려줘", "궁금해", "상담해줘"
        };

        foreach (var t in triggers)
            if (playerInput.Contains(t, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
}
