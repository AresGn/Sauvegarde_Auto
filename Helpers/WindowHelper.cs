using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AutosavePro.Helpers
{
    /// <summary>
    /// Fournit des méthodes pour interagir avec les fenêtres du système Windows
    /// </summary>
    public class WindowHelper
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private readonly string _appName;
        private readonly string _processName;

        public WindowHelper(string appName, string processName)
        {
            _appName = appName;
            _processName = processName;
        }

        /// <summary>
        /// Obtient toutes les fenêtres visibles pour un processus spécifique
        /// </summary>
        public List<WindowInfo> GetVisibleWindowsForProcess(int processId)
        {
            var result = new List<WindowInfo>();

            EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    uint winProcessId;
                    GetWindowThreadProcessId(hWnd, out winProcessId);

                    if (winProcessId == processId && IsWindowVisible(hWnd))
                    {
                        StringBuilder title = new StringBuilder(256);
                        GetWindowText(hWnd, title, 256);
                        string windowTitle = title.ToString();

                        if (!string.IsNullOrEmpty(windowTitle))
                        {
                            result.Add(new WindowInfo
                            {
                                Handle = hWnd,
                                Title = windowTitle,
                                ProcessId = processId
                            });
                            
                            Logger.Log($"Fenêtre trouvée: \"{windowTitle}\" (PID: {processId})", LogLevel.Debug);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Erreur lors de l'énumération des fenêtres: {ex.Message}", LogLevel.Error);
                }
                return true;
            }, IntPtr.Zero);

            return result;
        }

        /// <summary>
        /// Vérifie si le processus est en cours d'exécution
        /// </summary>
        public bool IsProcessRunning()
        {
            try
            {
                var processes = Process.GetProcessesByName(_processName);
                return processes.Length > 0;
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la vérification du processus {_processName}: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Obtient toutes les fenêtres pour une application
        /// </summary>
        public List<WindowInfo> GetAllWindowsForApp()
        {
            var windows = new List<WindowInfo>();
            var processes = Process.GetProcessesByName(_processName);
            
            Logger.Log($"Recherche de fenêtres pour {_appName} ({_processName}) - {processes.Length} processus trouvés", LogLevel.Debug);

            foreach (var process in processes)
            {
                try
                {
                    var processWindows = GetVisibleWindowsForProcess(process.Id);
                    windows.AddRange(processWindows);
                }
                finally
                {
                    process.Dispose();
                }
            }

            if (windows.Count == 0)
            {
                Logger.Log($"Aucune fenêtre trouvée pour {_appName}", LogLevel.Debug);
            }
            else
            {
                Logger.Log($"{windows.Count} fenêtre(s) trouvée(s) pour {_appName}", LogLevel.Debug);
            }

            return windows;
        }

        /// <summary>
        /// Obtient la fenêtre principale (MainWindow) pour un processus
        /// </summary>
        public WindowInfo? GetMainWindow(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                if (process != null && process.MainWindowHandle != IntPtr.Zero)
                {
                    StringBuilder title = new StringBuilder(256);
                    GetWindowText(process.MainWindowHandle, title, 256);
                    
                    return new WindowInfo
                    {
                        Handle = process.MainWindowHandle,
                        Title = title.ToString(),
                        ProcessId = processId
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de l'obtention de la fenêtre principale pour PID {processId}: {ex.Message}", LogLevel.Error);
            }
            
            return null;
        }

        /// <summary>
        /// Vérifie si une fenêtre appartient au processus spécifié
        /// </summary>
        public bool IsWindowFromProcess(IntPtr windowHandle, int processId)
        {
            uint winProcessId;
            GetWindowThreadProcessId(windowHandle, out winProcessId);
            return winProcessId == processId;
        }
    }

    /// <summary>
    /// Représente les informations d'une fenêtre Windows
    /// </summary>
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; } = "";
        public int ProcessId { get; set; }

        public override string ToString()
        {
            return $"{Title} (PID: {ProcessId})";
        }
    }
} 