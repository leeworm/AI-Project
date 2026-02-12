using System;

[Serializable]
public class WorldState
{
    public int day = 1;
    public string timeSlot = "morning"; // morning/afternoon/evening/night
    public string locationId = "home";  // 필요하면 확장
}
