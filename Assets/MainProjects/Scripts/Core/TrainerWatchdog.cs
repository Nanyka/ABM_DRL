using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Sugarscape
{
    // Ends a built training environment once its trainer is gone.
    // ML-Agents only calls Application.Quit() on disconnect, which has left the headless build running and growing in memory.
    public static class TrainerWatchdog
    {
        private const string PortArg = "--mlagents-port";
        private const int ProbeIntervalMs = 5000;
        private const int MaxFailedProbes = 3;
        private const int QuitGraceMs = 5000;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (Application.isEditor) return;

            var port = ReadTrainerPort();
            if (port <= 0) return; // not launched by a trainer

            Application.quitting += () => KillAfter(QuitGraceMs);
            new Thread(() => WatchTrainer(port)) { IsBackground = true, Name = "TrainerWatchdog" }.Start();
        }

        private static int ReadTrainerPort()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == PortArg && int.TryParse(args[i + 1], out var port))
                    return port;
            return -1;
        }

        private static void WatchTrainer(int port)
        {
            var failedProbes = 0;
            while (failedProbes < MaxFailedProbes)
            {
                Thread.Sleep(ProbeIntervalMs);
                failedProbes = IsTrainerListening(port) ? 0 : failedProbes + 1;
            }

            Debug.Log($"Trainer on port {port} is gone. Exiting.");
            Process.GetCurrentProcess().Kill();
        }

        private static bool IsTrainerListening(int port)
        {
            try
            {
                using (new TcpClient("127.0.0.1", port)) return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        private static void KillAfter(int delayMs)
        {
            new Thread(() =>
            {
                Thread.Sleep(delayMs);
                Process.GetCurrentProcess().Kill();
            }) { IsBackground = true }.Start();
        }
    }
}
