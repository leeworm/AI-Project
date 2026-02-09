using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Core
{
    /// <summary>
    /// 프로젝트 부팅/씬 전환의 최소 골격.
    /// - Boot 씬에 배치
    /// - 실행 시 initialSceneName(Home)으로 자동 진입
    /// - DontDestroyOnLoad로 유지
    /// </summary>
    public sealed class AppManager : MonoBehaviour
    {
        // 씬 이름은 문자열 오타가 가장 흔한 사고 원인이라 "한 곳"에만 모아 둡니다.
        public static class Scenes
        {
            public const string Boot = "Boot";
            public const string Home = "Home";
            public const string World = "World";
            public const string DemoDay = "DemoDay";
        }

        private static AppManager s_instance;

        [Header("Bootstrap")]
        [SerializeField] private string initialSceneName = Scenes.Home;
        [SerializeField] private int targetFrameRate = 60;

        [Header("Debug")]
        [SerializeField] private bool loadInitialSceneOnPlay = true;

        private bool _isLoading;

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = targetFrameRate;
        }

        private IEnumerator Start()
        {
            if (!loadInitialSceneOnPlay)
                yield break;

            // Boot 씬에서 시작하면 initialSceneName으로 진입
            // (혹시 다른 씬에서 실행해도 동일하게 동작하도록 조건 최소화)
            yield return LoadSceneAsync(initialSceneName);
        }

        /// <summary>
        /// 외부에서 씬 전환이 필요할 때 호출용(추후 UI 버튼/게임루프에서 사용)
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (_isLoading)
                return;

            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[AppManager] Scene name is null/empty.");
                yield break;
            }

            if (_isLoading)
                yield break;

            _isLoading = true;

            // 같은 씬이면 재로딩 안 함(원하면 주석 해제)
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                _isLoading = false;
                yield break;
            }

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[AppManager] Failed to load scene: {sceneName}");
                _isLoading = false;
                yield break;
            }

            while (!op.isDone)
                yield return null;

            _isLoading = false;
        }
    }
}
