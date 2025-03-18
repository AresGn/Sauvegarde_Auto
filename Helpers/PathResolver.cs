using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutosavePro.Helpers
{
    /// <summary>
    /// Résout les chemins de fichier à partir de noms extraits des titres de fenêtres
    /// </summary>
    public class PathResolver
    {
        private readonly string _appName;
        private readonly string _watchPath;
        private readonly List<string> _fileExtensions;
        private readonly List<(string path, int priority)> _searchPaths;
        private readonly bool _searchInSubdirectories;
        private readonly bool _createTempIfNotFound;

        public PathResolver(
            string appName, 
            string watchPath, 
            List<string> fileExtensions, 
            List<(string path, int priority)> searchPaths, 
            bool searchInSubdirectories = false,
            bool createTempIfNotFound = true)
        {
            _appName = appName;
            _watchPath = watchPath;
            _fileExtensions = fileExtensions;
            _searchPaths = searchPaths;
            _searchInSubdirectories = searchInSubdirectories;
            _createTempIfNotFound = createTempIfNotFound;
        }

        /// <summary>
        /// Résout le chemin complet d'un fichier à partir de son nom ou chemin partiel
        /// </summary>
        public string? ResolvePath(string extractedPath)
        {
            if (string.IsNullOrEmpty(extractedPath))
            {
                Logger.Log($"Impossible de résoudre un chemin vide", LogLevel.Debug);
                return null;
            }

            Logger.Log($"Résolution du chemin pour: \"{extractedPath}\"", LogLevel.Debug);
            
            // Étape 1: Vérifier si c'est déjà un chemin complet
            if (Path.IsPathRooted(extractedPath) && File.Exists(extractedPath))
            {
                Logger.Log($"Chemin complet trouvé: {extractedPath}", LogLevel.Info);
                return extractedPath;
            }
            
            // Étape 2: Obtenir les chemins de recherche ordonnés par priorité
            var orderedSearchPaths = _searchPaths
                .OrderByDescending(p => p.priority)
                .Select(p => p.path)
                .ToList();
            
            // Ajouter le watchPath avec haute priorité s'il n'est pas déjà présent
            if (!string.IsNullOrEmpty(_watchPath) && 
                !orderedSearchPaths.Any(p => string.Equals(p, _watchPath, StringComparison.OrdinalIgnoreCase)))
            {
                orderedSearchPaths.Insert(0, _watchPath);
            }
            
            // Ajouter Documents si pas déjà présent (priorité basse)
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!orderedSearchPaths.Any(p => string.Equals(p, documentsPath, StringComparison.OrdinalIgnoreCase)))
            {
                orderedSearchPaths.Add(documentsPath);
            }
            
            // Étape 3: Recherche dans les chemins prioritaires
            foreach (var searchPath in orderedSearchPaths)
            {
                try
                {
                    Logger.Log($"Recherche de \"{extractedPath}\" dans {searchPath}", LogLevel.Debug);
                    
                    // 3.1: Essayer le chemin direct
                    string potentialPath = Path.Combine(searchPath, extractedPath);
                    if (File.Exists(potentialPath))
                    {
                        Logger.Log($"Fichier trouvé au chemin direct: {potentialPath}", LogLevel.Info);
                        return potentialPath;
                    }
                    
                    // 3.2: Essayer avec différentes extensions
                    // Si le fichier extrait n'a pas déjà une extension connue
                    string currentExtension = Path.GetExtension(extractedPath).ToLowerInvariant();
                    bool hasKnownExtension = _fileExtensions.Contains(currentExtension);
                    
                    if (!hasKnownExtension)
                    {
                        foreach (var extension in _fileExtensions)
                        {
                            string pathWithExtension = Path.Combine(searchPath, extractedPath + extension);
                            if (File.Exists(pathWithExtension))
                            {
                                Logger.Log($"Fichier trouvé avec extension {extension}: {pathWithExtension}", LogLevel.Info);
                                return pathWithExtension;
                            }
                        }
                    }
                    
                    // 3.3: Recherche récursive dans les sous-dossiers (option configurable)
                    if (_searchInSubdirectories)
                    {
                        try
                        {
                            // Limiter la profondeur de recherche pour éviter de parcourir tout le disque
                            var foundFiles = Directory.GetFiles(searchPath, 
                                Path.GetFileName(extractedPath) + "*", 
                                new EnumerationOptions 
                                { 
                                    RecurseSubdirectories = true,
                                    MaxRecursionDepth = 3  // Éviter une recherche trop profonde
                                });
                            
                            if (foundFiles.Length > 0)
                            {
                                Logger.Log($"Fichier trouvé par recherche récursive: {foundFiles[0]}", LogLevel.Info);
                                return foundFiles[0];
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"Erreur lors de la recherche récursive: {ex.Message}", LogLevel.Debug);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Erreur lors de la recherche dans {searchPath}: {ex.Message}", LogLevel.Warning);
                }
            }
            
            // Étape 4: Si toujours pas trouvé, créer un fichier temporaire si l'option est activée
            if (_createTempIfNotFound)
            {
                return CreateTemporaryFile(extractedPath);
            }
            
            Logger.Log($"Aucun fichier trouvé pour {extractedPath}", LogLevel.Warning);
            return null;
        }

        /// <summary>
        /// Crée un fichier temporaire si aucun fichier n'est trouvé
        /// </summary>
        private string? CreateTemporaryFile(string fileName)
        {
            try
            {
                // Nettoyer le nom de fichier pour s'assurer qu'il est valide
                string safeFileName = CleanInvalidFileNameChars(fileName);
                
                // Déterminer l'extension à utiliser
                string extension = _fileExtensions.FirstOrDefault() ?? ".tmp";
                
                // Ajouter l'extension si nécessaire
                if (!safeFileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    safeFileName += extension;
                
                // Créer le dossier temporaire pour les fichiers non trouvés
                string tempFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "AutosavePro",
                    "FichiersNonSauvegardes",
                    CleanInvalidFileNameChars(_appName));
                    
                Directory.CreateDirectory(tempFolder);
                string tempFilePath = Path.Combine(tempFolder, safeFileName);
                
                // Créer le fichier temporaire s'il n'existe pas
                if (!File.Exists(tempFilePath))
                {
                    File.WriteAllBytes(tempFilePath, new byte[0]);
                    Logger.Log($"Création d'un fichier temporaire pour {_appName}: {tempFilePath}", LogLevel.Info);
                }
                
                return tempFilePath;
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la création du fichier temporaire: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Nettoie un nom de fichier en remplaçant les caractères invalides
        /// </summary>
        private string CleanInvalidFileNameChars(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unknown_file.txt";
                
            // Remplacer les caractères invalides par des underscores
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string cleanName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            
            // Si le nom commence par un point d'interrogation ou autre caractère spécial, le remplacer
            if (cleanName.StartsWith("?") || cleanName.StartsWith(".") || cleanName.StartsWith("_"))
                cleanName = "file_" + cleanName.TrimStart('?', '.', '_');
                
            // S'assurer que le nom n'est pas vide après nettoyage
            if (string.IsNullOrEmpty(cleanName))
                return "unknown_file.txt";
                
            return cleanName;
        }
    }
} 