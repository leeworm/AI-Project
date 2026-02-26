using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class ProxyAutoLauncher : MonoBehaviour
{
    private static ProxyAutoLauncher instance;

    [Header("Proxy executable path (absolute or project-relative)")]
    [SerializeField]
    private string proxyExePath =
        @"C:\Users\kalro\NpcProxy\bin\Release\net9.0\win-x64\publish\NpcProxy.exe";

    [Header("Proxy URL")]
    [SerializeField]
    private string proxyUrl = "http://127.0.0.1:5000";

    [Header("Optional: set API key here only for local dev (NOT for shipping)")]
    [SerializeField]
    private string devApiKey = ""; // 빌드용에서는 비우는 걸 권장

    private Process _process;
    private const int ProxyPort = 5000;

    private void Awake()
    {
        // 싱글톤 가드: 씬을 여러 번 로드해도 하나만 남도록
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        StartProxy();
    }

    private void OnApplicationQuit()
    {
        StopProxy();
    }

    private void StartProxy()
    {
        // 이미 내가 띄운 프로세스가 살아있으면 재실행 안 함
        if (_process != null && !_process.HasExited)
        {
            UnityEngine.Debug.Log("[Proxy] 이미 실행 중입니다.");
            return;
        }

        // 포트 점유 체크 (다른 프로그램이 5000 쓰고 있으면 새로 안 띄움)
        if (IsPortInUse(ProxyPort))
        {
            UnityEngine.Debug.LogWarning($"[Proxy] 포트 {ProxyPort}는 이미 사용 중입니다. " +
                             "이미 다른 프록시가 실행 중인 것으로 가정합니다.");
            return;
        }

        // 실행 파일 경로 정리
        var fullPath = ResolveExePath(proxyExePath);
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
        {
            UnityEngine.Debug.LogError($"[Proxy] 실행 파일을 찾을 수 없습니다: {fullPath}");
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
            WorkingDirectory = Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory
        };

        // OPENAI_API_KEY 설정
        var envApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envApiKey))
        {
            psi.EnvironmentVariables["OPENAI_API_KEY"] = envApiKey;
            UnityEngine.Debug.Log($"[Proxy] 환경 변수 OPENAI_API_KEY 감지 (length={envApiKey.Length}).");
        }
        else if (!string.IsNullOrWhiteSpace(devApiKey))
        {
            psi.EnvironmentVariables["OPENAI_API_KEY"] = devApiKey;
            UnityEngine.Debug.LogWarning("[Proxy] devApiKey를 사용 중입니다. 배포용 빌드에서는 비워두어야 합니다.");
        }
        else
        {
            UnityEngine.Debug.LogWarning("[Proxy] OPENAI_API_KEY가 설정되어 있지 않습니다.");
        }

        try
        {
            _process = new Process { StartInfo = psi };
            _process.OutputDataReceived += OnProxyOutputDataReceived;
            _process.ErrorDataReceived += OnProxyErrorDataReceived;

            if (!_process.Start())
            {
                UnityEngine.Debug.LogError("[Proxy] 프로세스를 시작하지 못했습니다.");
                _process.Dispose();
                _process = null;
                return;
            }

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            UnityEngine.Debug.Log("[Proxy] 프록시 프로세스를 시작했습니다.");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[Proxy] 시작 중 예외 발생: {ex.Message}");
            _process = null;
        }
    }

    private void StopProxy()
    {
        if (_process == null)
            return;

        try
        {
            if (!_process.HasExited)
            {
                UnityEngine.Debug.Log("[Proxy] 프록시 종료 시도.");
                _process.Kill();
                _process.WaitForExit(2000);
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("[Proxy] 종료 중 예외: " + e.Message);
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    /// <summary>
    /// proxyExePath에 절대경로/상대경로가 들어와도 처리하는 경로 보정 함수
    /// </summary>
    private static string ResolveExePath(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return string.Empty;

        // 이미 절대 경로면 그대로 사용
        if (Path.IsPathRooted(rawPath))
            return rawPath;

        // 프로젝트 루트 기준 상대 경로 (Assets 상위 폴더)
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.GetFullPath(Path.Combine(projectRoot, rawPath));
    }

    /// <summary>
    /// 해당 포트가 이미 사용 중인지 확인
    /// </summary>
    private static bool IsPortInUse(int port)
    {
        TcpListener listener = null;
        try
        {
            listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            return false; // 바인딩 성공 → 사용 중 아님
        }
        catch (SocketException)
        {
            return true;  // 바인딩 실패 → 누가 쓰는 중
        }
        finally
        {
            listener?.Stop();
        }
    }

    private void OnProxyOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
            UnityEngine.Debug.Log("[Proxy] " + e.Data);
    }

    private void OnProxyErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
            UnityEngine.Debug.LogError("[Proxy][ERR] " + e.Data);
    }
}