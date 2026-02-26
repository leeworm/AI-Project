using UnityEngine;

public sealed class UIRoot : MonoBehaviour
{
    private static UIRoot _i;

    private void Awake()
    {
        if (_i != null) { Destroy(gameObject); return; }
        _i = this;
        DontDestroyOnLoad(gameObject);
    }
}