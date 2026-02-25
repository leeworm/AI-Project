using UnityEngine;

public class UIActions : MonoBehaviour
{
    public void GoMainMenu() => SceneRouter.I.Go(SceneId.MainMenu);

    // 홈/월드/장소 이동
    public void GoHomeHub() => SceneRouter.I.Go(SceneId.HomeHub);
    public void GoWorldMap()
    {
        Debug.Log("[UIActions] GoWorldMap clicked");
        SceneRouter.I.Go(SceneId.WorldMap);
    }
    public void GoCafe() => SceneRouter.I.Go(SceneId.LocationCafe);

    // 필요하면 계속 추가: GoChurch(), GoPark() ...
    public void GoOptions() => SceneRouter.I.Go(SceneId.Options);

    // 메뉴
    public void ContinueSlot1() => SceneRouter.I.Continue(1);
    public void NewGameSlot1() => SceneRouter.I.NewGame(1);
    public void Quit() => SceneRouter.I.UI_Quit();
}