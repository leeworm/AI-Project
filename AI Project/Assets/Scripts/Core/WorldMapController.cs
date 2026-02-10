using TMPro;
using UnityEngine;
using Project.Core;
public enum PlaceId
{
    Home,
    Cafe,
    Lab,
    Park,
    Office
}

namespace Project.UI
{
    public sealed class WorldMapController : MonoBehaviour
    {
        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private TextMeshProUGUI tooltipText;

        [Header("Location Panel")]
        [SerializeField] private GameObject locationPanelRoot;
        [SerializeField] private TextMeshProUGUI locationTitleText;

        private PlaceId _currentPlace;

        private void Awake()
        {
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
            if (locationPanelRoot != null) locationPanelRoot.SetActive(false);
        }

        public void ShowTooltip(string displayName, RectTransform anchor)
        {
            if (tooltipRoot == null || tooltipText == null) return;

            tooltipText.text = string.IsNullOrWhiteSpace(displayName)
                ? "장소"
                : displayName;

            tooltipRoot.SetActive(true);

            // 간단 버전: 툴팁을 마우스 위치에 띄우고 싶으면 별도 스크립트로 확장 가능
        }

        public void HideTooltip()
        {
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
        }

        public void EnterPlace(PlaceId placeId)
        {
            // 집으로 돌아가는 것도 "장소 선택"으로 처리
            if (placeId == PlaceId.Home)
            {
                AppManager.Instance.LoadScene(AppManager.Scenes.Home);
                return;
            }

            _currentPlace = placeId;

            // 이벤트 시작: 시간대 진행 잠금
            GameLoopManager.Instance.BeginEvent();

            // 장소 패널 오픈(지금은 간단히)
            if (locationPanelRoot != null) locationPanelRoot.SetActive(true);
            if (locationTitleText != null) locationTitleText.text = placeId.ToString();
        }

        // 장소 이벤트 종료 버튼에서 호출
        public void ClosePlaceEvent_AsMajorAction()
        {
            // "핵심 행동" 이벤트였다면 종료 시점에 소모
            GameLoopManager.Instance.ConsumeMajorAction();

            GameLoopManager.Instance.EndEvent();

            if (locationPanelRoot != null) locationPanelRoot.SetActive(false);
        }

        // 프리 액션 종료(핵심 행동 소모 없음)
        public void ClosePlaceEvent_AsFreeAction()
        {
            GameLoopManager.Instance.EndEvent();

            if (locationPanelRoot != null) locationPanelRoot.SetActive(false);
        }
    }
}
