using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public sealed class NpcProxyClient
{
    private readonly string _proxyUrl;

    public NpcProxyClient(string proxyUrl)
    {
        _proxyUrl = proxyUrl;
    }

    public async Task<string> PostJsonAsync(string json)
    {
        using var req = new UnityWebRequest(_proxyUrl, "POST");
        var bodyRaw = Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
        {
            var body = req.downloadHandler?.text ?? "";
            throw new System.Exception($"Proxy error: {req.responseCode} {req.error}\n{body}");
        }

        return req.downloadHandler.text;
    }
}
