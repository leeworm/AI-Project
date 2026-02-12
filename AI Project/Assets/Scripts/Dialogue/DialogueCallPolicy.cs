using System;

public static class DialogueCallPolicy
{
    public static bool ShouldCallGpt(string playerInput)
    {
        if (string.IsNullOrWhiteSpace(playerInput)) return false;

        string[] triggers =
        {
            "고백","사과","화해","결정","선택","퀘스트","의심","진실",
            "왜","어떻게","도와","계획","비밀","중요","지금 당장"
        };

        foreach (var t in triggers)
            if (playerInput.Contains(t, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
}
