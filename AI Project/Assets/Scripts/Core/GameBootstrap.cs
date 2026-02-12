using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    public static GameState State { get; private set; }

    private void Awake()
    {
        if (State != null) return;

        State = new GameState();

        if (SaveManager.Instance != null)
            SaveManager.Instance.LoadAll(State);
        else
            Debug.LogWarning("SaveManager.Instance is null. Load skipped.");
    }

    private void SaveNow()
    {
        if (SaveManager.Instance != null && State != null)
            SaveManager.Instance.SaveAll(State);
    }

    // 에디터에서 Play Stop 할 때도 호출되는 경우가 많아 저장 트리거로 씁니다.
    private void OnDisable()
    {
#if UNITY_EDITOR
        SaveNow();
#endif
    }

    // 빌드 환경(실행 파일)에서 종료 시점 저장
    private void OnApplicationQuit()
    {
        SaveNow();
    }
}
