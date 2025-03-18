using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutosavePro.Helpers
{
    /// <summary>
    /// Gère la surveillance des fichiers pour une application
    /// </summary>
    public class FileWatcherManager : IDisposable
    {
        private readonly string _appName;
        private readonly string _watchPath;
        private readonly HashSet<string> _modifiedFiles;
        private readonly FileSystemWatcher _fileWatcher;
        private readonly ApplicationConfig _config;
        private readonly object _lockObject = new object();
        private bool _disposed;

        public event FileSystemEventHandler FileChanged;
        public event RenamedEventHandler FileRenamed;

        public FileWatcherManager(string appName, string watchPath, ApplicationConfig config)
        {
            _appName = appName;
            _watchPath = watchPath;
            _config = config;
            _modifiedFiles = new HashSet<string>();

            // Initialiser le FileSystemWatcher
            _fileWatcher = new FileSystemWatcher(_watchPath)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size
            };

            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Renamed += OnFileRenamed;

            Logger.Log($"Surveillance des fichiers initialisée pour {_appName} dans {_watchPath}");
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Vérifier que le fichier existe
            if (!File.Exists(e.FullPath)) return;

            try
            {
                // Ignorer les dossiers de l'application elle-même
                if (e.FullPath.Contains("AutosavePro"))
                {
                    return;
                }

                // Vérifier si c'est un fichier utilisateur valide
                if (!IsUserFile(e.FullPath)) return;

                lock (_lockObject)
                {
                    if (!_modifiedFiles.Contains(e.FullPath))
                    {
                        _modifiedFiles.Add(e.FullPath);
                        Logger.Log($"Fichier modifié dans {_appName}: {Path.GetFileName(e.FullPath)}");
                    }
                }

                // Déclencher l'événement
                FileChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la détection des modifications pour {_appName}: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                // Vérifier si c'est un fichier utilisateur valide
                if (!IsUserFile(e.FullPath)) return;

                lock (_lockObject)
                {
                    _modifiedFiles.Add(e.FullPath);
                    Logger.Log($"Fichier renommé dans {_appName}: {Path.GetFileName(e.OldFullPath)} -> {Path.GetFileName(e.FullPath)}");
                }

                // Déclencher l'événement
                FileRenamed?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la détection des renommages pour {_appName}: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Vérifie si un fichier est un fichier utilisateur valide (et non un fichier temporaire)
        /// </summary>
        public bool IsUserFile(string filePath)
        {
            try
            {
                // Ignorer les fichiers vides ou non valides
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return false;
                }

                string fileName = Path.GetFileName(filePath).ToLower();
                string extension = Path.GetExtension(filePath).ToLower();

                // Ignorer les fichiers temporaires selon l'application
                switch (_appName)
                {
                    case "Microsoft Word":
                        if (filePath.Contains("~WR") || 
                            fileName.StartsWith("~$") ||
                            extension == ".tmp" ||
                            fileName.Contains("~wrd"))
                        {
                            Logger.Log($"Fichier temporaire Word ignoré: {filePath}", LogLevel.Debug);
                            return false;
                        }
                        break;

                    case "Microsoft Excel":
                        if (fileName.Length == 8 && fileName.All(c => "0123456789ABCDEF".Contains(c)) || 
                            fileName.StartsWith("~$") ||
                            extension == ".tmp" ||
                            fileName.Contains("~xl"))
                        {
                            Logger.Log($"Fichier temporaire Excel ignoré: {filePath}", LogLevel.Debug);
                            return false;
                        }
                        break;

                    case "Microsoft PowerPoint":
                        if (fileName.StartsWith("~$") || 
                            extension == ".tmp" ||
                            fileName.Contains("~ppt"))
                        {
                            Logger.Log($"Fichier temporaire PowerPoint ignoré: {filePath}", LogLevel.Debug);
                            return false;
                        }
                        break;

                    default:
                        // Règles génériques pour les fichiers temporaires
                        if (fileName.StartsWith("~") || 
                            fileName.StartsWith(".") || 
                            extension == ".tmp" || 
                            extension == ".bak" ||
                            fileName.Contains("temp"))
                        {
                            Logger.Log($"Fichier temporaire ignoré: {filePath}", LogLevel.Debug);
                            return false;
                        }
                        break;
                }

                // Vérifier si on peut effectivement accéder au fichier
                try
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // On peut ouvrir le fichier, donc on peut le sauvegarder
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Fichier inaccessible ignoré: {filePath} - {ex.Message}", LogLevel.Debug);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la vérification du fichier {filePath}: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Récupère et vide la liste des fichiers modifiés
        /// </summary>
        public List<string> GetAndClearModifiedFiles()
        {
            lock (_lockObject)
            {
                var files = _modifiedFiles.ToList();
                _modifiedFiles.Clear();
                return files;
            }
        }

        /// <summary>
        /// Ajoute un fichier à la liste des fichiers modifiés
        /// </summary>
        public void AddModifiedFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            lock (_lockObject)
            {
                if (!_modifiedFiles.Contains(filePath))
                {
                    _modifiedFiles.Add(filePath);
                    Logger.Log($"Fichier ajouté manuellement à la liste des fichiers modifiés: {filePath}");
                }
            }
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
                }
                _disposed = true;
            }
        }
    }
} 