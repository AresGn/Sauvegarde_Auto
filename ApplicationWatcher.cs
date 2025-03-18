using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Timers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using AutosavePro.Helpers;

namespace AutosavePro
{
    public class ApplicationWatcher : IDisposable
    {
        // Définition des constantes pour l'envoi de touches
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_S = 0x53;
        
        // Autres constantes Win32
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // Structures et constantes pour SendInput
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION union;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mouseInput;
            [FieldOffset(0)]
            public KEYBDINPUT keyboardInput;
            [FieldOffset(0)]
            public HARDWAREINPUT hardwareInput;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        public event EventHandler<string>? OnFileOpened;

        public string AppName { get; private set; }
        public string ProcessName { get; private set; }
        public string WatchPath { get; private set; }
        private bool _disposed;
        private TimeSpan _autoSaveInterval = TimeSpan.FromMinutes(1);
        
        // Services utilisés par ApplicationWatcher
        private readonly WindowHelper _windowHelper;
        private readonly TitleParser _titleParser;
        private readonly PathResolver _pathResolver;
        private readonly KeyboardManager _keyboardManager;
        private readonly FileWatcherManager _fileWatcher;
        private readonly AutoSaveTimer _autoSaveTimer;
        
        // Configuration de l'application
        private ApplicationConfig? _config;

        public ApplicationWatcher(string appName, string processName, string watchPath)
        {
            AppName = appName;
            ProcessName = processName;
            WatchPath = watchPath;
            _disposed = false;

            // Initialiser la configuration
            _config = ApplicationRegistry.Instance.GetConfigByAppName(appName);
            if (_config == null)
            {
                // Si la configuration n'existe pas, en créer une de base et l'ajouter au registre
                _config = new ApplicationConfig(appName, processName);
                ApplicationRegistry.Instance.AddApplicationConfig(_config);
            }

            // Initialiser les services d'assistance
            _windowHelper = new WindowHelper(appName, processName);
            
            _titleParser = new TitleParser(
                appName,
                _config.FileTitlePattern,
                _config.TitlePrefix,
                _config.TitleSuffix,
                _config.ModificationIndicators);
            
            // Convertir les chemins de recherche pour le PathResolver
            var searchPaths = new List<(string path, int priority)>();
            if (_config.AdditionalSearchPaths != null)
            {
                foreach (var pathEntry in _config.AdditionalSearchPaths)
                {
                    if (pathEntry is KeyValuePair<string, int> kvp)
                    {
                        searchPaths.Add((kvp.Key, kvp.Value));
                    }
                    else if (pathEntry is string path)
                    {
                        searchPaths.Add((path, 50));
                    }
                }
            }
            
            _pathResolver = new PathResolver(
                appName,
                watchPath,
                _config.FileExtensions,
                searchPaths,
                _config.SearchInSubdirectories,
                _config.CreateTempFileIfNotFound);
            
            _keyboardManager = new KeyboardManager(
                appName,
                _config.InitialDelay,
                _config.FinalDelay,
                _config.UseAdvancedSaveMethod);

            // Initialiser FileWatcherManager
            _fileWatcher = new FileWatcherManager(appName, watchPath, _config);
            _fileWatcher.FileChanged += OnFileChangedHandler;
            _fileWatcher.FileRenamed += OnFileRenamedHandler;

            // Initialiser AutoSaveTimer
            _autoSaveTimer = new AutoSaveTimer(appName, _autoSaveInterval);
            _autoSaveTimer.TimerElapsed += OnAutoSaveTimerElapsed;
        }

        private void OnFileChangedHandler(object sender, FileSystemEventArgs e)
        {
            try {
                if (!_windowHelper.IsProcessRunning()) return;
                
                // Pour Excel, attendre un court instant avant de tenter la sauvegarde
                if (AppName == "Microsoft Excel")
                {
                    Thread.Sleep(500); // Attendre que Excel finisse ses opérations temporaires
                }
                
                // Tenter une sauvegarde en place (Ctrl+S)
                if (SaveActiveFile())
                {
                    OnFileOpened?.Invoke(this, e.FullPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors du traitement du changement de fichier: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnFileRenamedHandler(object sender, RenamedEventArgs e)
        {
            try {
                if (!_windowHelper.IsProcessRunning()) return;
                
                // Pour Excel, attendre un court instant avant de tenter la sauvegarde
                if (AppName == "Microsoft Excel")
                {
                    Thread.Sleep(500); // Attendre que Excel finisse ses opérations temporaires
                }
                
                // Tenter une sauvegarde
                SaveActiveFile();
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors du traitement du renommage de fichier: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnAutoSaveTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (!_windowHelper.IsProcessRunning()) return;
                
                Logger.Log($"Vérification périodique pour {AppName}", LogLevel.Debug);
                
                // Tenter d'abord une sauvegarde en place (Ctrl+S)
                if (SaveActiveFile())
                {
                    // Après la sauvegarde en place, mettre à jour la copie de sauvegarde
                    var openFiles = GetOpenedFiles();
                    if (openFiles.Any())
                    {
                        foreach (var file in openFiles)
                        {
                            OnFileOpened?.Invoke(this, file);
                            _fileWatcher.AddModifiedFile(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la vérification périodique pour {AppName}: {ex.Message}", LogLevel.Error);
            }
        }

        public bool IsApplicationRunning()
        {
            return _windowHelper.IsProcessRunning();
        }

        public List<string> GetAndClearModifiedFiles()
        {
            return _fileWatcher.GetAndClearModifiedFiles();
        }

        public List<string> GetOpenedFiles()
        {
            var openedFiles = new List<string>();
            var windows = _windowHelper.GetAllWindowsForApp();
            
            foreach (var window in windows)
            {
                if (!string.IsNullOrEmpty(window.Title))
                {
                    string? filePath = GetFilePathFromTitle(window.Title);
                    if (filePath != null && File.Exists(filePath))
                    {
                        openedFiles.Add(filePath);
                        Logger.Log($"Fichier détecté et ajouté: {filePath}");
                    }
                    else
                    {
                        Logger.Log($"Impossible d'extraire un chemin de fichier valide depuis: \"{window.Title}\"");
                    }
                }
            }

            if (openedFiles.Count == 0)
            {
                Logger.Log($"Aucun fichier ouvert détecté pour {AppName}");
            }
            else
            {
                Logger.Log($"{openedFiles.Count} fichier(s) ouvert(s) détecté(s) pour {AppName}");
            }

            return openedFiles;
        }

        public string? GetFilePathFromTitle(string windowTitle)
        {
            if (string.IsNullOrEmpty(windowTitle))
                return null;

            // Étape 1: Extraire le nom du fichier ou chemin depuis le titre
            string? extractedPath = _titleParser.ExtractFilePathFromTitle(windowTitle);
            if (string.IsNullOrEmpty(extractedPath))
            {
                Logger.Log($"Impossible d'extraire un nom de fichier depuis le titre: \"{windowTitle}\"", LogLevel.Debug);
                return null;
            }

            // Étape 2: Résoudre le chemin complet
            return _pathResolver.ResolvePath(extractedPath);
        }

        public void SetAutoSaveInterval(TimeSpan interval)
        {
            _autoSaveInterval = interval;
            _autoSaveTimer.SetInterval(interval);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _fileWatcher?.Dispose();
                    _autoSaveTimer?.Dispose();
                }
                _disposed = true;
            }
        }
        
        // Méthode pour trouver la fenêtre et envoyer Ctrl+S
        public bool SaveActiveFile()
        {
            try
            {
                if (!_windowHelper.IsProcessRunning())
                {
                    Logger.Log($"Aucun processus {ProcessName} trouvé", LogLevel.Debug);
                    return false;
                }
                
                var windows = _windowHelper.GetAllWindowsForApp();
                if (windows.Count == 0)
                {
                    Logger.Log($"Aucune fenêtre trouvée pour {AppName}", LogLevel.Debug);
                    return false;
                }
                
                bool success = false;
                
                foreach (var window in windows)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(window.Title))
                        {
                            bool shouldSave = false;
                            
                            // Déterminer si on doit sauvegarder cette fenêtre
                            if (_config != null)
                            {
                                // Vérifier si le titre correspond à un fichier modifié selon la configuration
                                shouldSave = _titleParser.IsModifiedTitle(window.Title);
                                
                                // Vérifier si le titre n'est pas dans la liste d'exclusion
                                if (shouldSave && _config.IsExcludedTitle(window.Title))
                                    shouldSave = false;
                            }
                            else
                            {
                                // Fallback si pas de configuration
                                shouldSave = window.Title.Contains("*") || 
                                           window.Title.Contains("•") ||
                                           window.Title.Contains("(modifié)") ||
                                           window.Title.Contains("non enregistré");
                            }
                            
                            if (shouldSave)
                            {
                                Logger.Log($"Sauvegarde de {AppName}: \"{window.Title}\"", LogLevel.Info);
                                
                                // Tenter la sauvegarde avec le KeyboardManager
                                bool saveSuccess = _keyboardManager.SendCtrlS(window.Handle);
                                
                                if (saveSuccess)
                                {
                                    Logger.Log($"Sauvegarde réussie pour {AppName}", LogLevel.Info);
                                    success = true;
                                }
                                else
                                {
                                    Logger.Log($"Échec de la sauvegarde pour {AppName}", LogLevel.Warning);
                                }
                            }
                            else
                            {
                                Logger.Log($"Pas besoin de sauvegarder {AppName} (pas de modifications détectées)", LogLevel.Debug);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Erreur lors du traitement de la fenêtre pour {AppName}: {ex.Message}", LogLevel.Error);
                    }
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la sauvegarde pour {AppName}: {ex.Message}", LogLevel.Error);
                return false;
            }
        }
    }
}