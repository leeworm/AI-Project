using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.UI
{
    public sealed class MapHotspot : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private PlaceId placeId;
        [SerializeField] private GameObject highlight; // 테두리/글로우 오브젝트(선택)
        [SerializeField] private string displayName;   // 툴팁 표시명(선택)

        private WorldMapController _controller;

        private void Awake()
        {
            _controller = GetComponentInParent<WorldMapController>();
            if (highlight != null) highlight.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (highlight != null) highlight.SetActive(true);
            _controller?.ShowTooltip(displayName, transform as RectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (highlight != null) highlight.SetActive(false);
            _controller?.HideTooltip();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _controller?.EnterPlace(placeId);
        }
    }
}
