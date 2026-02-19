using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSlotMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "NpcApiTest"; // 실제 게임 씬 이름으로 변경

    public void ContinueSlot(int slot)
    {
        SaveManager.Instance.SetSlot(slot);
        SceneManager.LoadScene(gameSceneName);
    }

    public void NewGameSlot(int slot)
    {
        SaveManager.Instance.DeleteSlot(slot);
        SaveManager.Instance.SetSlot(slot);
        SceneManager.LoadScene(gameSceneName);
    }
}
