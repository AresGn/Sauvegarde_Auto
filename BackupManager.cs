using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AutosavePro
{
    public class BackupManager : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private readonly string _backupBasePath;
        private readonly Dictionary<string, ApplicationWatcher> _watchedApplications;
        private readonly System.Timers.Timer _backupTimer = new();
        private bool _isEnabled = true;
        private TimeSpan _backupInterval;
        private bool _disposed;

        public BackupManager(string backupPath)
        {
            _backupBasePath = backupPath;
            _watchedApplications = new Dictionary<string, ApplicationWatcher>();
            _backupInterval = TimeSpan.FromMinutes(5);
            InitializeBackupTimer();

            if (!Directory.Exists(_backupBasePath))
            {
                Directory.CreateDirectory(_backupBasePath);
                Logger.Log($"Dossier de sauvegarde créé : {_backupBasePath}");
            }
        }

        private void InitializeBackupTimer()
        {
            _backupTimer.Interval = _backupInterval.TotalMilliseconds;
            _backupTimer.Elapsed += OnBackupTimerElapsed;
            _backupTimer.Start();
            Logger.Log($"Minuteur de sauvegarde initialisé ({_backupInterval.TotalMinutes} minutes)");
        }

        private void OnBackupTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                // Vérifier et créer le dossier de sauvegarde s'il n'existe pas
                if (!Directory.Exists(_backupBasePath))
                {
                    Directory.CreateDirectory(_backupBasePath);
                    Logger.Log($"Création du dossier de sauvegarde: {_backupBasePath}", LogLevel.Info);
                }

                // Pour chaque application surveillée, faire une sauvegarde
                foreach (var watcher in _watchedApplications.Values)
                {
                    // Ignorer si l'application n'est pas en cours d'exécution
                    if (!watcher.IsApplicationRunning())
                    {
                        continue;
                    }

                    try
                    {
                        // 1. Récupérer et sauvegarder les fichiers modifiés
                        var modifiedFiles = watcher.GetAndClearModifiedFiles();
                        if (modifiedFiles.Count > 0)
                        {
                            Logger.Log($"[{watcher.AppName}] {modifiedFiles.Count} fichiers modifiés à sauvegarder", LogLevel.Info);
                            BackupModifiedFiles(watcher, modifiedFiles);
                        }

                        // 2. Tenter de sauvegarder les fichiers actuellement ouverts
                            BackupOpenFiles(watcher);
                        }
                    catch (Exception ex)
                    {
                        Logger.Log($"Erreur lors de la sauvegarde pour {watcher.AppName}: {ex.Message}", LogLevel.Error);
                    }
                }

                Logger.Log("Cycle de sauvegarde terminé", LogLevel.Info);
                }
                catch (Exception ex)
                {
                Logger.Log($"Erreur dans le timer de sauvegarde: {ex.Message}", LogLevel.Error);
            }
        }

        private void BackupModifiedFiles(ApplicationWatcher watcher, List<string> modifiedFiles)
        {
            try
            {
                // Filtrer les fichiers non valides ou inaccessibles avant de commencer
                var validFiles = new List<string>();
                foreach (string filePath in modifiedFiles)
                {
                    // Ignorer les fichiers temporaires ou de notre propre application
                    if (filePath.Contains("~$") || 
                        filePath.Contains(".tmp") || 
                        filePath.Contains("AutosavePro"))
                    {
                        continue;
                    }

                    // Vérification basique d'existence
                    if (!File.Exists(filePath))
                    {
                        Logger.Log($"Fichier inexistant, ignoré: {filePath}", LogLevel.Warning);
                        continue;
                    }
                    
                    // Vérifier les chemins système à éviter
                    string[] systemPaths = {
                        @"C:\Windows\",
                        @"C:\Program Files\",
                        @"C:\Program Files (x86)\",
                        @"C:\ProgramData\"
                    };
                    
                    bool isSystemPath = false;
                    foreach (var path in systemPaths)
                    {
                        if (filePath.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Log($"Fichier système ignoré: {filePath}", LogLevel.Debug);
                            isSystemPath = true;
                            break;
                        }
                    }
                    
                    if (isSystemPath) continue;
                    
                    // Vérifier les droits d'accès
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        // Test d'accès en lecture
                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            // Fichier accessible
                        }
                        
                        // Ajouter le fichier à la liste valide s'il n'y est pas déjà
                        if (!validFiles.Contains(filePath))
                        {
                            validFiles.Add(filePath);
                            Logger.Log($"Fichier validé pour sauvegarde: {filePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Fichier inaccessible, ignoré: {filePath} - {ex.Message}", LogLevel.Warning);
                    }
                }
                
                // Continuer avec les fichiers valides uniquement
                if (validFiles.Count == 0)
                {
                    Logger.Log("Aucun fichier valide à sauvegarder", LogLevel.Debug);
                    return;
                }
                
                // Créer un dossier de sauvegarde clair et visible
                string backupRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "SauvegardeAuto"
                );
                
                // Utiliser le chemin configuré s'il est absolu, sinon utiliser le chemin par défaut
                if (Path.IsPathRooted(_backupBasePath))
                {
                    backupRoot = _backupBasePath;
                }
                
                foreach (string filePath in validFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    string backupFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_backup{Path.GetExtension(fileName)}";
                    
                    // Chemin structuré par application
                    string backupPath = Path.Combine(backupRoot, watcher.AppName);
                    string backupFilePath = Path.Combine(backupPath, backupFileName);

                    // S'assurer que le dossier de sauvegarde existe
                    Directory.CreateDirectory(backupPath);

                    Logger.Log($"Mise à jour de la sauvegarde de {fileName}...", LogLevel.Debug);

                    try
                    {
                        // Méthode directe si possible
                        if (TryCopyFile(filePath, backupFilePath))
                        {
                            Logger.Log($"Sauvegarde mise à jour: {fileName}", LogLevel.Info);
                            continue; // Passer au fichier suivant
                        }
                        
                        // Méthode alternative avec fichier temporaire
                        string tempFilePath = Path.GetTempFileName();
                        using (FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (FileStream tempStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                        {
                            sourceStream.CopyTo(tempStream);
                        }
                        
                        File.Copy(tempFilePath, backupFilePath, true);
                        File.Delete(tempFilePath);

                        Logger.Log($"Sauvegarde mise à jour: {fileName}", LogLevel.Info);
                    }
                    catch (IOException ex)
                    {
                        Logger.Log($"Erreur d'accès au fichier {fileName}: {ex.Message}", LogLevel.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la sauvegarde: {ex.Message}", LogLevel.Error);
            }
        }
        
        // Méthode utilitaire pour essayer de copier un fichier directement
        private bool TryCopyFile(string source, string destination)
        {
            try
            {
                File.Copy(source, destination, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void BackupOpenFiles(ApplicationWatcher watcher)
        {
            try
            {
                Logger.Log($"Recherche de fichiers ouverts pour {watcher.AppName}...");
                
                // Méthode 1: Utiliser la fonction GetOpenedFiles de ApplicationWatcher
                var openedFiles = watcher.GetOpenedFiles();
                
                if (openedFiles.Any())
                {
                    Logger.Log($"{openedFiles.Count} fichier(s) trouvé(s) via GetOpenedFiles");
                    BackupModifiedFiles(watcher, openedFiles);
                    return;
                }
                
                // Méthode 2: Vérifier directement les fenêtres principales
                Logger.Log("Tentative avec méthode alternative...");
                var processes = Process.GetProcessesByName(watcher.ProcessName);
                foreach (var process in processes)
                {
                    try
                    {
                        IntPtr foregroundWindow = GetForegroundWindow();
                        uint processId;
                        GetWindowThreadProcessId(foregroundWindow, out processId);

                        if (processId == process.Id)
                        {
                            StringBuilder title = new StringBuilder(256);
                            GetWindowText(foregroundWindow, title, 256);
                            string windowTitle = title.ToString();

                            Logger.Log($"Fenêtre au premier plan: \"{windowTitle}\"");

                            if (!string.IsNullOrEmpty(windowTitle))
                            {
                                string? filePath = GetFilePathFromTitle(windowTitle, watcher);
                                if (filePath != null && File.Exists(filePath))
                                {
                                    Logger.Log($"Fichier détecté au premier plan: {filePath}");
                                    BackupModifiedFiles(watcher, new List<string> { filePath });
                                }
                                else if (watcher.AppName == "Bloc-notes" && windowTitle.EndsWith(" - Bloc-notes"))
                                {
                                    // Pour le Bloc-notes spécifiquement, si on ne trouve pas le fichier avec GetFilePathFromTitle,
                                    // essayons une méthode plus directe pour le Bloc-notes
                                    string fileName = windowTitle.Replace(" - Bloc-notes", "");
                                    Logger.Log($"Tentative spéciale Bloc-notes pour fichier: {fileName}");
                                    
                                    // Chercher dans les dossiers courants
                                    string[] searchPaths = new[] {
                                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                        watcher.WatchPath
                                    };
                                    
                                    foreach (var searchPath in searchPaths)
                                    {
                                        try {
                                            var files = Directory.GetFiles(searchPath, Path.GetFileName(fileName), SearchOption.AllDirectories);
                                            if (files.Length > 0)
                                            {
                                                Logger.Log($"Fichier Bloc-notes trouvé: {files[0]}");
                                                BackupModifiedFiles(watcher, new List<string> { files[0] });
                                                break;
                                            }
                                        } catch (Exception) {
                                            // Ignorer les erreurs de recherche
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la recherche de fichiers ouverts : {ex.Message}", LogLevel.Error);
            }
        }

        private string? GetFilePathFromTitle(string windowTitle, ApplicationWatcher watcher)
        {
            if (string.IsNullOrEmpty(windowTitle))
                return null;
                
            // Déléguer à la méthode de l'ApplicationWatcher qui est maintenant plus robuste
            var potentialPath = watcher.GetFilePathFromTitle(windowTitle);
            if (potentialPath != null) {
                return potentialPath;
            }
            
            // Si la méthode de ApplicationWatcher n'a pas trouvé le fichier, utiliser notre propre implémentation
            if (watcher.AppName == "Bloc-notes")
            {
                // Détecter les différentes variantes du titre du bloc-notes
                string modifiedTitle = windowTitle;
                
                // Gérer différents types de tirets et formats de titre
                if (windowTitle.Contains(" - Bloc-notes") || 
                    windowTitle.Contains(" – Bloc-notes") || 
                    windowTitle.Contains("- Bloc-notes") ||
                    windowTitle.Contains("– Bloc-notes"))
                {
                    // Standardiser le format
                    modifiedTitle = windowTitle
                        .Replace(" – Bloc-notes", "")
                        .Replace(" - Bloc-notes", "")
                        .Replace("– Bloc-notes", "")
                        .Replace("- Bloc-notes", "");
                    
                    // Enlever l'astérisque qui indique un fichier non sauvegardé
                    if (modifiedTitle.StartsWith("*"))
                    {
                        modifiedTitle = modifiedTitle.Substring(1);
                        Logger.Log($"Détection d'un fichier modifié et non sauvegardé: {modifiedTitle}");
                    }
                    
                    Logger.Log($"[BackupManager] Titre normalisé : \"{modifiedTitle}\"");
                    
                    // Créer un fichier temporaire pour les fichiers non sauvegardés
                    if (modifiedTitle.Contains("."))
                    {
                        try {
                            string tempFolder = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                "SauvegardeAuto", "FichiersNonSauvegardes");
                                
                            Directory.CreateDirectory(tempFolder);
                            string tempFilePath = Path.Combine(tempFolder, modifiedTitle);
                            
                            // Si le fichier n'existe pas encore, le créer
                            if (!File.Exists(tempFilePath))
                            {
                                File.WriteAllText(tempFilePath, "");
                                Logger.Log($"[BackupManager] Création d'un fichier temporaire pour un document non sauvegardé: {tempFilePath}");
                            }
                            
                            return tempFilePath;
                        } catch (Exception ex) {
                            Logger.Log($"Erreur lors de la création du fichier temporaire: {ex.Message}", LogLevel.Error);
                        }
                    }
                }
            }
            
            // Ajouter d'autres applications ici si nécessaire
            return null;
        }

        public void AddApplication(string appName, string processName, string watchPath)
        {
            if (!_watchedApplications.ContainsKey(appName))
            {
                var watcher = new ApplicationWatcher(appName, processName, watchPath);
                watcher.OnFileOpened += (sender, fileName) =>
                {
                    if (_isEnabled)
                    {
                        Logger.Log($"Sauvegarde initiale du fichier {Path.GetFileName(fileName)}");
                        var files = new List<string> { fileName };
                        BackupModifiedFiles(watcher, files);
                    }
                };
                _watchedApplications.Add(appName, watcher);
                Logger.Log($"Application ajoutée à la surveillance : {appName}");
            }
        }

        public void RemoveApplication(string appName)
        {
            if (_watchedApplications.ContainsKey(appName))
            {
                _watchedApplications[appName].Dispose();
                _watchedApplications.Remove(appName);
                Logger.Log($"Application retirée de la surveillance : {appName}");
            }
        }

        public void SetBackupInterval(int minutes)
        {
            _backupInterval = TimeSpan.FromMinutes(minutes);
            _backupTimer.Interval = _backupInterval.TotalMilliseconds;
            
            // Mettre à jour l'intervalle pour tous les ApplicationWatcher
            foreach (var watcher in _watchedApplications.Values)
            {
                watcher.SetAutoSaveInterval(_backupInterval);
            }
            
            Logger.Log($"Intervalle de sauvegarde mis à jour : {minutes} minutes", LogLevel.Info);
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            _backupTimer.Enabled = enabled;
            Logger.Log($"Sauvegarde automatique {(enabled ? "activée" : "désactivée")}");
        }

        public IEnumerable<ApplicationWatcher> GetWatchedApplications()
        {
            return _watchedApplications.Values;
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
                    // Arrêter le timer
                    _backupTimer.Stop();
                    _backupTimer.Dispose();

                    // Disposer tous les ApplicationWatcher
                    foreach (var watcher in _watchedApplications.Values)
                    {
                        watcher.Dispose();
                    }
                    _watchedApplications.Clear();
                }
                _disposed = true;
            }
        }

        ~BackupManager()
        {
            Dispose(false);
        }

        private void CreateBackupDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                    Logger.Log($"Création du dossier de sauvegarde: {directoryPath}", LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Erreur lors de la création du dossier {directoryPath}: {ex.Message}", LogLevel.Error, 
                        "AutosavePro");
                }
            }
        }
    }
}