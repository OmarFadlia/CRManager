using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CRManager.Client.Maui.Services;

public static class BackendProcessManager
{
    private static Process? _apiProcess;
    private static readonly object _lock = new();

    public static void StartBackendApiIfRequired()
    {
#if WINDOWS
        Task.Run(async () =>
        {
            try
            {
                // 1. Check if backend API is already running and responding
                if (await IsBackendRunningAsync())
                {
                    return;
                }

                // 2. Locate the API executable
                var apiExePath = FindApiExecutable();
                if (string.IsNullOrEmpty(apiExePath) || !File.Exists(apiExePath))
                {
                    Debug.WriteLine($"[BackendProcessManager] Warning: Could not locate CRManager.Api.exe");
                    return;
                }

                lock (_lock)
                {
                    if (_apiProcess != null && !_apiProcess.HasExited)
                        return;

                    var workingDir = Path.GetDirectoryName(apiExePath) ?? "";

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = apiExePath,
                        WorkingDirectory = workingDir,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    _apiProcess = Process.Start(startInfo);
                }

                // Register process exit hook
                AppDomain.CurrentDomain.ProcessExit += (s, e) => StopBackendApi();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackendProcessManager] Error starting API: {ex.Message}");
            }
        });
#endif
    }

    public static void StopBackendApi()
    {
#if WINDOWS
        lock (_lock)
        {
            try
            {
                if (_apiProcess != null && !_apiProcess.HasExited)
                {
                    Debug.WriteLine("[BackendProcessManager] Stopping child backend API process...");
                    _apiProcess.Kill(entireProcessTree: true);
                    _apiProcess.Dispose();
                    _apiProcess = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackendProcessManager] Error stopping API: {ex.Message}");
            }
        }
#endif
    }

    private static async Task<bool> IsBackendRunningAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
            var response = await client.GetAsync("http://localhost:5283/api/CreditCards");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindApiExecutable()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 1. Search in solution project build output directories
        var dir = new DirectoryInfo(baseDir);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "CRManager.Api", "bin", "Debug", "net8.0", "CRManager.Api.exe");
            if (File.Exists(candidate)) return candidate;

            candidate = Path.Combine(dir.FullName, "src", "CRManager.Api", "bin", "Release", "net8.0", "CRManager.Api.exe");
            if (File.Exists(candidate)) return candidate;

            candidate = Path.Combine(dir.FullName, "CRManager.Api", "bin", "Debug", "net8.0", "CRManager.Api.exe");
            if (File.Exists(candidate)) return candidate;

            dir = dir.Parent;
        }

        // 2. Production distribution fallback (when all binaries are deployed into the same directory)
        var localCandidate = Path.Combine(baseDir, "CRManager.Api.exe");
        if (File.Exists(localCandidate)) return localCandidate;

        return null;
    }
}
