using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutosavePro
{
    /// <summary>
    /// Registre central des configurations d'applications
    /// </summary>
    public class ApplicationRegistry
    {
        private readonly Dictionary<string, ApplicationConfig> _appConfigs = new();
        
        // Singleton
        private static readonly Lazy<ApplicationRegistry> _instance = 
            new Lazy<ApplicationRegistry>(() => new ApplicationRegistry());
            
        public static ApplicationRegistry Instance => _instance.Value;
        
        private ApplicationRegistry()
        {
            InitializeDefaultConfigurations();
        }
        
        /// <summary>
        /// Initialise les configurations par défaut pour les applications courantes
        /// </summary>
        private void InitializeDefaultConfigurations()
        {
            // Microsoft Word
            var wordConfig = new ApplicationConfig("Microsoft Word", "WINWORD");
            wordConfig.ExclusionPatterns.AddRange(new[] { "Document1", "Document2" });
            wordConfig.FileTitlePattern = @"^(?<filepath>.+?)(?<modified>\s*[*]|\s*\[Lecture seule\])? - Word$";
            wordConfig.AdditionalSearchPaths.Add(new KeyValuePair<string, int>(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 90));
            wordConfig.FileExtensions.AddRange(new[] { ".docx", ".doc", ".rtf" });
            wordConfig.SearchInSubdirectories = true;
            AddApplicationConfig(wordConfig);
            
            // Microsoft Excel
            var excelConfig = new ApplicationConfig("Microsoft Excel", "EXCEL");
            excelConfig.ExclusionPatterns.AddRange(new[] { "Classeur1", "Classeur2" });
            excelConfig.FileTitlePattern = @"^(?<filepath>.+?)(?<modified>\s*[*]|\s*\[Lecture seule\])? - Excel$";
            excelConfig.InitialDelay = 50;
            excelConfig.FinalDelay = 250;
            excelConfig.AdditionalSearchPaths.Add(new KeyValuePair<string, int>(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 90));
            excelConfig.FileExtensions.AddRange(new[] { ".xlsx", ".xls", ".csv" });
            excelConfig.SearchInSubdirectories = true;
            AddApplicationConfig(excelConfig);
            
            // Microsoft PowerPoint
            var pptConfig = new ApplicationConfig("Microsoft PowerPoint", "POWERPNT");
            pptConfig.ExclusionPatterns.AddRange(new[] { "Présentation1", "Présentation2" });
            pptConfig.FileTitlePattern = @"^(?<filepath>.+?)(?<modified>\s*[*]|\s*\[Lecture seule\])? - PowerPoint$";
            pptConfig.InitialDelay = 50;
            pptConfig.FinalDelay = 250;
            pptConfig.AdditionalSearchPaths.Add(new KeyValuePair<string, int>(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 90));
            pptConfig.FileExtensions.AddRange(new[] { ".pptx", ".ppt" });
            pptConfig.SearchInSubdirectories = true;
            AddApplicationConfig(pptConfig);
            
            // Bloc-notes
            var notepadConfig = new ApplicationConfig("Bloc-notes", "notepad");
            notepadConfig.FileTitlePattern = @"^(?<filepath>.+?)(?<modified>\s*[*])? - Bloc-notes$";
            notepadConfig.InitialDelay = 20;
            notepadConfig.FinalDelay = 100;
            notepadConfig.AdditionalSearchPaths.AddRange(new object[] {
                new KeyValuePair<string, int>(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 80),
                new KeyValuePair<string, int>(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 70)
            });
            notepadConfig.FileExtensions.AddRange(new[] { ".txt", ".log", ".ini" });
            AddApplicationConfig(notepadConfig);
            
            // Visual Studio Code
            var vscodeConfig = new ApplicationConfig("Visual Studio Code", "Code");
            vscodeConfig.ModificationIndicators.Add("●");
            vscodeConfig.FileTitlePattern = @"^(?<filepath>.+?) [-—] (?<workspace>.+) [-—] Visual Studio Code$";
            vscodeConfig.InitialDelay = 30;
            vscodeConfig.FinalDelay = 150;
            vscodeConfig.UseAdvancedSaveMethod = true;
            vscodeConfig.AdditionalSearchPaths.AddRange(new object[] {
                new KeyValuePair<string, int>(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 60),
                new KeyValuePair<string, int>(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source"), 80)
            });
            vscodeConfig.FileExtensions.AddRange(new[] { 
                ".txt", ".html", ".css", ".js", ".ts", ".json", ".cs", ".py", ".md", 
                ".xml", ".yml", ".yaml", ".sh", ".ps1", ".java", ".php", ".config" 
            });
            vscodeConfig.SearchInSubdirectories = true;
            AddApplicationConfig(vscodeConfig);
            
            // Notepad++
            var notepadPlusConfig = new ApplicationConfig("Notepad++", "notepad++");
            notepadPlusConfig.ModificationIndicators.Add("*");
            notepadPlusConfig.TitleSuffix = " - Notepad++";
            notepadPlusConfig.FileTitlePattern = @"^(?<filepath>.+?)(?<modified>\s*[*])? - Notepad\+\+$";
            notepadPlusConfig.InitialDelay = 30;
            notepadPlusConfig.FinalDelay = 150;
            notepadPlusConfig.FileExtensions.AddRange(new[] { 
                ".txt", ".log", ".xml", ".html", ".css", ".js", ".php", ".sql", ".ini", ".conf" 
            });
            notepadPlusConfig.SearchInSubdirectories = true;
            AddApplicationConfig(notepadPlusConfig);
        }
        
        /// <summary>
        /// Ajoute une configuration d'application au registre
        /// </summary>
        public void AddApplicationConfig(ApplicationConfig config)
        {
            if (_appConfigs.ContainsKey(config.AppName))
            {
                _appConfigs[config.AppName] = config;
            }
            else
            {
                _appConfigs.Add(config.AppName, config);
            }
            Logger.Log($"Configuration ajoutée pour {config.AppName}", LogLevel.Debug);
        }
        
        /// <summary>
        /// Récupère la configuration d'une application par son nom
        /// </summary>
        public ApplicationConfig? GetConfigByAppName(string appName)
        {
            if (_appConfigs.TryGetValue(appName, out var config))
            {
                return config;
            }
            return null;
        }
        
        /// <summary>
        /// Récupère la configuration d'une application par son nom de processus
        /// </summary>
        public ApplicationConfig? GetConfigByProcessName(string processName)
        {
            return _appConfigs.Values.FirstOrDefault(c => 
                c.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Supprime une configuration d'application du registre
        /// </summary>
        public bool RemoveApplicationConfig(string appName)
        {
            return _appConfigs.Remove(appName);
        }
        
        /// <summary>
        /// Récupère toutes les configurations d'applications
        /// </summary>
        public IEnumerable<ApplicationConfig> GetAllConfigurations()
        {
            return _appConfigs.Values;
        }

        /// <summary>
        /// Détermine si l'application doit afficher le dialogue de bienvenue au démarrage
        /// </summary>
        public bool ShouldShowWelcomeDialog()
        {
            // Logique pour déterminer si le dialogue de bienvenue doit être affiché
            // Pour l'instant, retourne toujours faux pour éviter d'afficher le dialogue
            return false;
        }
    }
} 