using System.IO;
using UnityEngine;

public static class JsonSave
{
    public static void Save<T>(string path, T data)
    {
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(path, json);
    }

    public static bool TryLoad<T>(string path, out T data)
    {
        data = default;
        if (!File.Exists(path)) return false;

        var json = File.ReadAllText(path);
        data = JsonUtility.FromJson<T>(json);
        return true;
    }
}
