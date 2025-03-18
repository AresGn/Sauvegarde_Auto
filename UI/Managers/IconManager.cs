using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using AutosavePro.Helpers;

namespace AutosavePro.UI.Managers
{
    /// <summary>
    /// Gestionnaire des icônes pour les applications
    /// </summary>
    public class IconManager
    {
        private readonly ImageList _appImageList;
        private readonly Dictionary<string, int> _iconCache = new Dictionary<string, int>();
        
        /// <summary>
        /// Obtient la liste d'images utilisée pour stocker les icônes
        /// </summary>
        public ImageList AppImageList => _appImageList;
        
        public IconManager()
        {
            // Création de la liste d'images pour les icônes
            _appImageList = new ImageList();
            _appImageList.ImageSize = new Size(24, 24);
            _appImageList.ColorDepth = ColorDepth.Depth32Bit;
        }
        
        /// <summary>
        /// Ajoute une icône à la liste d'images et retourne son index
        /// </summary>
        public int AddIcon(string key, Icon icon)
        {
            if (_iconCache.TryGetValue(key, out int existingIndex))
            {
                return existingIndex;
            }
            
            _appImageList.Images.Add(key, icon);
            int index = _appImageList.Images.Count - 1;
            _iconCache[key] = index;
            return index;
        }
        
        /// <summary>
        /// Obtient l'index d'une icône pour une application installée
        /// </summary>
        public int GetIconIndex(InstalledApplication app)
        {
            // Si l'application n'a pas d'icône, retourner -1
            if (app == null || app.AppIcon == null)
                return -1;
                
            string key = app.Name;
            
            // Vérifier si l'icône est déjà dans le cache
            if (_iconCache.TryGetValue(key, out int existingIndex))
            {
                return existingIndex;
            }
            
            // Ajouter l'icône et retourner son index
            return AddIcon(key, app.AppIcon);
        }
        
        /// <summary>
        /// Obtient l'index d'une icône pour une application surveillée
        /// </summary>
        public int GetIconIndexForWatchedApp(ApplicationWatcher app)
        {
            if (app == null)
                return -1;
                
            // Extraire le nom de base de l'application (sans les suffixes)
            string baseAppName = app.AppName
                .Replace(" (Documents)", "")
                .Replace(" (Bureau)", "");
                
            // Vérifier si l'icône est déjà dans le cache
            if (_iconCache.TryGetValue(baseAppName, out int existingIndex))
            {
                return existingIndex;
            }
            
            // Sinon, essayer de trouver l'icône parmi les applications installées
            var installedApps = new InstalledAppsManager().GetInstalledApplications();
            
            // Rechercher l'application correspondante
            var matchingApp = installedApps.FirstOrDefault(a => 
                a.Name.Equals(app.AppName, StringComparison.OrdinalIgnoreCase) ||
                a.Name.Equals(baseAppName, StringComparison.OrdinalIgnoreCase));
            
            // Cas spécial pour VS Code qui peut avoir différents noms
            if (baseAppName == "Visual Studio Code" && matchingApp == null)
            {
                matchingApp = installedApps.FirstOrDefault(a => 
                    a.Name.Contains("Visual Studio Code") || 
                    a.Name.Contains("VS Code") ||
                    a.ExecutablePath?.Contains("Code.exe") == true);
            }
            
            // Si une application correspondante est trouvée et qu'elle a une icône
            if (matchingApp != null && matchingApp.AppIcon != null)
            {
                return AddIcon(baseAppName, matchingApp.AppIcon);
            }
            
            // Sinon, utiliser une icône par défaut
            return GetOrAddSystemIcon(GetExtensionForApp(baseAppName));
        }
        
        /// <summary>
        /// Obtient l'extension associée à une application
        /// </summary>
        private string GetExtensionForApp(string appName)
        {
            if (appName.Contains("Word"))
                return "docx";
            if (appName.Contains("Excel"))
                return "xlsx";
            if (appName.Contains("PowerPoint"))
                return "pptx";
            if (appName.Contains("OneNote"))
                return "one";
            if (appName.Contains("Bloc-notes"))
                return "txt";
            if (appName.Contains("Visual Studio Code"))
                return "code";
                
            return "app";
        }
        
        /// <summary>
        /// Obtient ou ajoute une icône système pour une extension donnée
        /// </summary>
        public int GetOrAddSystemIcon(string extension)
        {
            string key = $"system_{extension}";
            
            // Vérifier si l'icône est déjà dans le cache
            if (_iconCache.TryGetValue(key, out int existingIndex))
            {
                return existingIndex;
            }
            
            // Sinon, essayer d'obtenir l'icône système
            try
            {
                Icon? icon = null;
                
                // Obtenir l'icône selon l'extension
                switch (extension)
                {
                    case "docx":
                    case "xlsx":
                    case "pptx":
                    case "one":
                    case "txt":
                    case "code":
                    case "app":
                    default:
                        icon = SystemIcons.Application;
                        break;
                }
                
                if (icon != null)
                {
                    return AddIcon(key, icon);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de l'obtention de l'icône système pour {extension}: {ex.Message}", LogLevel.Error);
            }
            
            return -1;
        }
        
        /// <summary>
        /// Vide le cache d'icônes
        /// </summary>
        public void ClearIconCache()
        {
            _iconCache.Clear();
            _appImageList.Images.Clear();
        }
    }
} 