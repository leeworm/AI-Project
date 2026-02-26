using UnityEngine;
using UnityEngine.UI;

public sealed class QuestPanelController : MonoBehaviour
{
    [System.Serializable]
    public struct RectState
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 offsetMin;
        public Vector2 offsetMax;

        public static RectState Capture(RectTransform rt)
        {
            return new RectState
            {
                anchorMin = rt.anchorMin,
                anchorMax = rt.anchorMax,
                pivot = rt.pivot,
                anchoredPosition = rt.anchoredPosition,
                sizeDelta = rt.sizeDelta,
                offsetMin = rt.offsetMin,
                offsetMax = rt.offsetMax
            };
        }

        public void Apply(RectTransform rt)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }

    [Header("Refs")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button toggleButton;

    [Header("Optional (collapse 시 숨길 영역)")]
    [SerializeField] private GameObject bodyRoot; // Body 전체(목록 부분)

    [Header("States (Editor에서 캡처)")]
    [SerializeField] private RectState collapsed;
    [SerializeField] private RectState expanded;
    [SerializeField] private bool startExpanded;

    public bool IsExpanded => _expanded;
    private bool _expanded;

    private void Awake()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(Toggle);

        _expanded = startExpanded;
        ApplyState(_expanded);
    }

    public void Toggle()
    {
        _expanded = !_expanded;
        ApplyState(_expanded);
    }

    private void ApplyState(bool expandedState)
    {
        if (panel == null) return;

        if (bodyRoot != null)
            bodyRoot.SetActive(expandedState);

        if (expandedState) expanded.Apply(panel);
        else collapsed.Apply(panel);

        // 레이아웃/텍스트가 한 프레임 늦게 재배치되며 튀는 걸 방지
        ForceRebuild(panel);
    }

    private static void ForceRebuild(RectTransform root)
    {
        if (root == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        Canvas.ForceUpdateCanvases();
    }

#if UNITY_EDITOR
    [ContextMenu("Capture Collapsed From Current")]
    private void CaptureCollapsed()
    {
        if (panel == null) panel = (RectTransform)transform;
        collapsed = RectState.Capture(panel);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Capture Expanded From Current")]
    private void CaptureExpanded()
    {
        if (panel == null) panel = (RectTransform)transform;
        expanded = RectState.Capture(panel);
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}