using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class UIShowUtil
{
    public static IEnumerator ShowNoPop(GameObject panel)
    {
        if (panel == null) yield break;

        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        panel.SetActive(true);

        // TMP/레이아웃 계산 1프레임 대기
        yield return null;

        Canvas.ForceUpdateCanvases();
        var rt = panel.transform as RectTransform;
        if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        Canvas.ForceUpdateCanvases();

        cg.alpha = 1f;
    }

    public static void ForceRebuild(RectTransform rt)
    {
        if (!rt) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        Canvas.ForceUpdateCanvases();
    }
}