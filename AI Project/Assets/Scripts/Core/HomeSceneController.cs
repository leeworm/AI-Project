using UnityEngine;
using Project.Core;

public sealed class HomeSceneController : MonoBehaviour
{
    public void GoToWorld()
    {
        AppManager.Instance.LoadScene(AppManager.Scenes.World);
    }
}