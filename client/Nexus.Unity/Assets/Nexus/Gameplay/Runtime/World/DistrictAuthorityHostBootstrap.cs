using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Nexus.Gameplay.World
{
    // Development-only launcher for the local semantic authority. It tracks
    // the exact child PID it owns and never scans or kills unrelated processes.
    public sealed class DistrictAuthorityHostBootstrap : MonoBehaviour
    {
        [SerializeField] private bool launchOnStart = true;
        [SerializeField] private string executableOverride = string.Empty;
        [SerializeField, Min(1)] private int port = 43101;
        private Process ownedProcess;
        private bool exitReported;

        public int Port => port;
        public bool OwnsRunningProcess => ownedProcess != null && !ownedProcess.HasExited;

        private void Start()
        {
            if (launchOnStart) Launch();
        }

        private void Update()
        {
            if (ownedProcess != null && ownedProcess.HasExited && !exitReported)
            {
                exitReported = true;
                string error = ownedProcess.StandardError.ReadToEnd();
                Debug.LogWarning("District authority host exited (code " + ownedProcess.ExitCode + "): " + error, this);
            }
        }

        public bool Launch()
        {
            if (OwnsRunningProcess) return true;
            string executable = ResolveExecutable();
            if (string.IsNullOrEmpty(executable) || !File.Exists(executable))
            {
                Debug.LogWarning("District authority host executable was not found. Build Nexus.Host in Release first.", this);
                return false;
            }

            try
            {
                ownedProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "--district-host " + port,
                    WorkingDirectory = Path.GetDirectoryName(executable),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                exitReported = false;
                if (ownedProcess == null || (ownedProcess.WaitForExit(250) && ownedProcess.HasExited))
                {
                    string error = ownedProcess == null ? string.Empty : ownedProcess.StandardError.ReadToEnd();
                    Debug.LogWarning("District authority host exited immediately (code " + (ownedProcess == null ? "unknown" : ownedProcess.ExitCode.ToString()) + "): " + error, this);
                    return false;
                }
                return OwnsRunningProcess;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("District authority host launch failed: " + exception.Message, this);
                ownedProcess = null;
                return false;
            }
        }

        private string ResolveExecutable()
        {
            if (!string.IsNullOrWhiteSpace(executableOverride)) return Path.GetFullPath(executableOverride);
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) return string.Empty;
            string repositoryRoot = Directory.GetParent(projectRoot)?.Parent?.FullName;
            string[] candidates =
            {
                Path.Combine(projectRoot, "Nexus.Host.exe"),
                Path.Combine(repositoryRoot ?? projectRoot, "src", "Nexus.Host", "bin", "Release", "net9.0", "Nexus.Host.exe")
            };
            foreach (string candidate in candidates) if (File.Exists(candidate)) return candidate;
            return string.Empty;
        }

        private void OnDestroy()
        {
            if (ownedProcess == null) return;
            try
            {
                if (!ownedProcess.HasExited) ownedProcess.CloseMainWindow();
                if (!ownedProcess.HasExited) ownedProcess.Kill();
                ownedProcess.Dispose();
            }
            catch (InvalidOperationException) { }
            finally { ownedProcess = null; }
        }
    }
}
