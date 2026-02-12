using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class ProxyAutoLauncher : MonoBehaviour
{
    [Header("Proxy executable path (relative to project or absolute)")]
    [SerializeField]
    private string proxyExePath =
        @"C:\Users\kalro\NpcProxy\bin\Release\net9.0\win-x64\publish\NpcProxy.exe";

    [Header("Proxy URL")]
    [SerializeField] private string proxyUrl = "http://127.0.0.1:5000";

    [Header("Optional: set API key here only for local dev (NOT for shipping)")]
    [SerializeField] private string devApiKey = ""; // 비워두는 걸 권장

    private Process _process;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartProxy();
    }

    private void OnApplicationQuit()
    {
        StopProxy();
    }

    private void StartProxy()
    {
        if (_process != null && !_process.HasExited) return;

        var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, proxyExePath));
        if (!File.Exists(fullPath))
        {
            UnityEngine.Debug.LogError($"Proxy exe not found: {fullPath}");
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = fullPath,
            Arguments = $"--urls \"{proxyUrl}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        UnityEngine.Debug.Log($"Unity sees OPENAI_API_KEY length: {(string.IsNullOrWhiteSpace(apiKey) ? 0 : apiKey.Length)}");

        if (!string.IsNullOrWhiteSpace(apiKey))
            psi.EnvironmentVariables["OPENAI_API_KEY"] = apiKey;



        _process = Process.Start(psi);

        _process.OutputDataReceived += (_, e) => { if (e.Data != null) UnityEngine.Debug.Log("[Proxy] " + e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data != null) UnityEngine.Debug.LogError("[Proxy] " + e.Data); };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        UnityEngine.Debug.Log("Proxy started.");
    }

    private void StopProxy()
    {
        try
        {
            if (_process == null) return;
            if (_process.HasExited) return;

            _process.Kill();
            _process.WaitForExit(2000);
            _process.Dispose();
            _process = null;

            UnityEngine.Debug.Log("Proxy stopped.");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("Failed to stop proxy: " + e.Message);
        }
    }
}
