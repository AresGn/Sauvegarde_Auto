using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AutosavePro
{
    /// <summary>
    /// Configuration spécifique à une application pour les opérations de sauvegarde
    /// </summary>
    public class ApplicationConfig
    {
        // Identifiants de l'application
        public string AppName { get; set; } = "";
        public string ProcessName { get; set; } = "";
        
        // Paramètres pour l'envoi de raccourcis clavier
        public int InitialDelay { get; set; } = 50;
        public int FinalDelay { get; set; } = 200;
        
        // Indicateurs de fichier modifié dans le titre de la fenêtre
        public List<string> ModificationIndicators { get; set; } = new List<string>();
        
        // Motifs à exclure (lecture seule, mode protégé, etc.)
        public List<string> ExclusionPatterns { get; set; } = new List<string>();
        
        // Expression régulière pour extraire le chemin du fichier du titre
        public string? FileTitlePattern { get; set; }
        
        // Préfixe et suffixe spécifiques dans le titre de la fenêtre
        public string? TitlePrefix { get; set; }
        public string? TitleSuffix { get; set; }
        
        // Dossiers supplémentaires où chercher les fichiers
        public List<object> AdditionalSearchPaths { get; set; } = new List<object>();
        
        // Extensions de fichiers pour cette application
        public List<string> FileExtensions { get; set; } = new List<string>();
        
        // Indique si la recherche doit être faite dans les sous-dossiers
        public bool SearchInSubdirectories { get; set; } = false;
        
        // Indique si un fichier temporaire doit être créé si aucun n'est trouvé
        public bool CreateTempFileIfNotFound { get; set; } = true;
        
        // Méthode de sauvegarde avancée (SendInput au lieu de keybd_event)
        public bool UseAdvancedSaveMethod { get; set; } = false;
        
        // Nombre maximal de tentatives de sauvegarde
        public int MaxSaveAttempts { get; set; } = 2;
        
        public ApplicationConfig(string appName, string processName)
        {
            AppName = appName;
            ProcessName = processName;
            
            // Initialiser les indicateurs de modification par défaut
            ModificationIndicators.AddRange(new[] { "*", "•", "(modifié)", "non enregistré" });
            
            // Initialiser les motifs d'exclusion par défaut
            ExclusionPatterns.AddRange(new[] { "[Lecture seule]", "[Mode protégé]", "- [Protégé]" });
            
            // Configurer des extensions de fichiers selon l'application
            ConfigureDefaultSettings();
        }
        
        /// <summary>
        /// Vérifie si le titre de la fenêtre indique un fichier modifié
        /// </summary>
        public bool IsModifiedTitle(string windowTitle)
        {
            // Utiliser l'expression régulière si définie
            if (!string.IsNullOrEmpty(FileTitlePattern))
            {
                try
                {
                    var match = Regex.Match(windowTitle, FileTitlePattern);
                    if (match.Success && match.Groups["modified"].Success)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    // Ignorer les erreurs d'expression régulière et continuer
                }
            }
            
            // Utiliser les indicateurs de modification
            foreach (var indicator in ModificationIndicators)
            {
                if (windowTitle.Contains(indicator))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Vérifie si le titre de la fenêtre est exclu (lecture seule, etc.)
        /// </summary>
        public bool IsExcludedTitle(string windowTitle)
        {
            foreach (var pattern in ExclusionPatterns)
            {
                if (windowTitle.Contains(pattern))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Extrait le chemin du fichier à partir du titre de la fenêtre
        /// </summary>
        public string? ExtractFilePathFromTitle(string windowTitle)
        {
            // Si un pattern d'expression régulière est défini, l'utiliser
            if (!string.IsNullOrEmpty(FileTitlePattern))
            {
                try
                {
                    var match = Regex.Match(windowTitle, FileTitlePattern);
                    if (match.Success)
                    {
                        // Chercher le groupe nommé "filepath" ou le premier groupe capturé
                        if (match.Groups["filepath"].Success)
                        {
                            return match.Groups["filepath"].Value;
                        }
                        else if (match.Groups.Count > 1)
                        {
                            return match.Groups[1].Value;
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignorer les erreurs d'expression régulière et continuer avec l'approche alternative
                }
            }
            
            // Sinon, utiliser la méthode basée sur préfixe/suffixe
            if (!string.IsNullOrEmpty(TitlePrefix) && !string.IsNullOrEmpty(TitleSuffix))
            {
                int startPos = windowTitle.IndexOf(TitlePrefix);
                if (startPos >= 0)
                {
                    startPos += TitlePrefix.Length;
                    int endPos = windowTitle.IndexOf(TitleSuffix, startPos);
                    if (endPos > startPos)
                    {
                        return windowTitle.Substring(startPos, endPos - startPos);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(TitlePrefix))
            {
                int startPos = windowTitle.IndexOf(TitlePrefix);
                if (startPos >= 0)
                {
                    startPos += TitlePrefix.Length;
                    string result = windowTitle.Substring(startPos);
                    // Si un indicateur de modification est à la fin, le supprimer
                    foreach (var indicator in ModificationIndicators)
                    {
                        if (result.EndsWith(indicator))
                        {
                            return result.Substring(0, result.Length - indicator.Length).Trim();
                        }
                    }
                    return result;
                }
            }
            else if (!string.IsNullOrEmpty(TitleSuffix))
            {
                int endPos = windowTitle.IndexOf(TitleSuffix);
                if (endPos > 0)
                {
                    return windowTitle.Substring(0, endPos);
                }
            }
            else
            {
                // Approche générique basée sur les patterns communs des applications
                // si aucune configuration spécifique n'est définie
                
                // Microsoft Office: "Document - Application"
                Regex officePattern = new Regex(@"^(.+) - (Word|Excel|PowerPoint|Outlook)");
                var officeMatch = officePattern.Match(windowTitle);
                if (officeMatch.Success)
                {
                    return officeMatch.Groups[1].Value;
                }
                
                // Format Visual Studio Code: "filename - workspace - Visual Studio Code"
                Regex vscodePattern = new Regex(@"^(.+) - .+ - Visual Studio Code");
                var vscodeMatch = vscodePattern.Match(windowTitle);
                if (vscodeMatch.Success)
                {
                    return vscodeMatch.Groups[1].Value;
                }
                
                // Format Bloc-notes: "filename - Bloc-notes"
                Regex notepadPattern = new Regex(@"^(.+) - Bloc-notes");
                var notepadMatch = notepadPattern.Match(windowTitle);
                if (notepadMatch.Success)
                {
                    return notepadMatch.Groups[1].Value;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Configure les paramètres par défaut selon le type d'application
        /// </summary>
        private void ConfigureDefaultSettings()
        {
            // Configuration spécifique selon le type d'application
            if (AppName.Contains("Word") || ProcessName.Contains("WINWORD"))
            {
                // Configuration pour Microsoft Word
                FileExtensions.AddRange(new[] { ".docx", ".doc", ".rtf" });
                TitleSuffix = " - Word";
                FileTitlePattern = @"^(?<filepath>.+?)(?: [-*] | \[Lecture seule\])* - Word$";
                UseAdvancedSaveMethod = false;
            }
            else if (AppName.Contains("Excel") || ProcessName.Contains("EXCEL"))
            {
                // Configuration pour Microsoft Excel
                FileExtensions.AddRange(new[] { ".xlsx", ".xls", ".csv" });
                TitleSuffix = " - Excel";
                FileTitlePattern = @"^(?<filepath>.+?)(?<modified>\s*[*]|\s*\[Lecture seule\])? - Excel$";
                UseAdvancedSaveMethod = false;
            }
            else if (AppName.Contains("PowerPoint") || ProcessName.Contains("POWERPNT"))
            {
                // Configuration pour Microsoft PowerPoint
                FileExtensions.AddRange(new[] { ".pptx", ".ppt" });
                TitleSuffix = " - PowerPoint";
                FileTitlePattern = @"^(?<filepath>.+?)(?<modified>\s*[*]|\s*\[Lecture seule\])? - PowerPoint$";
                UseAdvancedSaveMethod = false;
            }
            else if (AppName.Contains("Visual Studio Code") || ProcessName.Contains("Code"))
            {
                // Configuration pour Visual Studio Code
                FileExtensions.AddRange(new[] { ".txt", ".html", ".css", ".js", ".ts", ".json", ".cs", ".py", ".md" });
                FileTitlePattern = @"^(?<filepath>.+?) - .+ - Visual Studio Code$";
                UseAdvancedSaveMethod = true;
                SearchInSubdirectories = true;
            }
            else if (AppName.Contains("Bloc-notes") || ProcessName.Contains("notepad"))
            {
                // Configuration pour Bloc-notes
                FileExtensions.AddRange(new[] { ".txt", ".log", ".ini", ".csv" });
                TitleSuffix = " - Bloc-notes";
                FileTitlePattern = @"^(?<filepath>.+?)(?<modified>\s*[*])? - Bloc-notes$";
                UseAdvancedSaveMethod = false;
            }
        }
    }
} 