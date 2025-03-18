using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AutosavePro.Helpers
{
    /// <summary>
    /// Extrait les informations de fichiers à partir des titres de fenêtres
    /// </summary>
    public class TitleParser
    {
        private readonly string _appName;
        private readonly string? _titlePattern;
        private readonly string? _titlePrefix;
        private readonly string? _titleSuffix;
        private readonly List<string> _modificationIndicators;

        public TitleParser(
            string appName, 
            string? titlePattern = null, 
            string? titlePrefix = null, 
            string? titleSuffix = null,
            List<string>? modificationIndicators = null)
        {
            _appName = appName;
            _titlePattern = titlePattern;
            _titlePrefix = titlePrefix;
            _titleSuffix = titleSuffix;
            _modificationIndicators = modificationIndicators ?? new List<string> { "*", "•", "(modifié)", "non enregistré" };
        }

        /// <summary>
        /// Extrait le chemin du fichier à partir du titre de la fenêtre
        /// </summary>
        public string? ExtractFilePathFromTitle(string windowTitle)
        {
            if (string.IsNullOrEmpty(windowTitle))
            {
                Logger.Log($"Titre de fenêtre vide, impossible d'extraire un nom de fichier", LogLevel.Debug);
                return null;
            }

            Logger.Log($"Extraction du chemin depuis: \"{windowTitle}\"", LogLevel.Debug);

            // Méthode 1: Utiliser l'expression régulière si définie
            if (!string.IsNullOrEmpty(_titlePattern))
            {
                try
                {
                    var match = Regex.Match(windowTitle, _titlePattern);
                    if (match.Success)
                    {
                        // Chercher le groupe nommé "filepath" ou le premier groupe capturé
                        if (match.Groups["filepath"].Success)
                        {
                            string result = match.Groups["filepath"].Value;
                            Logger.Log($"Chemin extrait avec regex (groupe nommé): \"{result}\"", LogLevel.Debug);
                            return result;
                        }
                        else if (match.Groups.Count > 1)
                        {
                            string result = match.Groups[1].Value;
                            Logger.Log($"Chemin extrait avec regex (groupe 1): \"{result}\"", LogLevel.Debug);
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Erreur lors de l'extraction avec regex: {ex.Message}", LogLevel.Warning);
                }
            }
            
            // Méthode 2: Utiliser le préfixe et le suffixe si définis
            if (!string.IsNullOrEmpty(_titlePrefix) && !string.IsNullOrEmpty(_titleSuffix))
            {
                int startPos = windowTitle.IndexOf(_titlePrefix);
                if (startPos >= 0)
                {
                    startPos += _titlePrefix.Length;
                    int endPos = windowTitle.IndexOf(_titleSuffix, startPos);
                    if (endPos > startPos)
                    {
                        string result = windowTitle.Substring(startPos, endPos - startPos);
                        Logger.Log($"Chemin extrait avec préfixe/suffixe: \"{result}\"", LogLevel.Debug);
                        return result;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(_titlePrefix))
            {
                int startPos = windowTitle.IndexOf(_titlePrefix);
                if (startPos >= 0)
                {
                    startPos += _titlePrefix.Length;
                    string result = windowTitle.Substring(startPos);
                    
                    // Si un indicateur de modification est à la fin, le supprimer
                    foreach (var indicator in _modificationIndicators)
                    {
                        if (result.EndsWith(indicator))
                        {
                            result = result.Substring(0, result.Length - indicator.Length).Trim();
                            break;
                        }
                    }
                    
                    Logger.Log($"Chemin extrait avec préfixe: \"{result}\"", LogLevel.Debug);
                    return result;
                }
            }
            else if (!string.IsNullOrEmpty(_titleSuffix))
            {
                int endPos = windowTitle.IndexOf(_titleSuffix);
                if (endPos > 0)
                {
                    string result = windowTitle.Substring(0, endPos);
                    Logger.Log($"Chemin extrait avec suffixe: \"{result}\"", LogLevel.Debug);
                    return result;
                }
            }
            
            // Méthode 3: Approche générique basée sur les patterns communs des applications
            // Microsoft Office: "Document - Application"
            Regex officePattern = new Regex(@"^(.+) - (Word|Excel|PowerPoint|Outlook)");
            var officeMatch = officePattern.Match(windowTitle);
            if (officeMatch.Success)
            {
                string result = officeMatch.Groups[1].Value;
                Logger.Log($"Chemin extrait avec pattern Office: \"{result}\"", LogLevel.Debug);
                return result;
            }
            
            // Format Visual Studio Code: "filename - workspace - Visual Studio Code"
            Regex vscodePattern = new Regex(@"^(.+) - .+ - Visual Studio Code");
            var vscodeMatch = vscodePattern.Match(windowTitle);
            if (vscodeMatch.Success)
            {
                string result = vscodeMatch.Groups[1].Value;
                Logger.Log($"Chemin extrait avec pattern VS Code: \"{result}\"", LogLevel.Debug);
                return result;
            }
            
            // Format Bloc-notes: "filename - Bloc-notes"
            Regex notepadPattern = new Regex(@"^(.+) - Bloc-notes");
            var notepadMatch = notepadPattern.Match(windowTitle);
            if (notepadMatch.Success)
            {
                string result = notepadMatch.Groups[1].Value;
                Logger.Log($"Chemin extrait avec pattern Bloc-notes: \"{result}\"", LogLevel.Debug);
                return result;
            }
            
            // Format Notepad++: "filename - Notepad++"
            Regex notepadPlusPattern = new Regex(@"^(.+) - Notepad\+\+");
            var notepadPlusMatch = notepadPlusPattern.Match(windowTitle);
            if (notepadPlusMatch.Success)
            {
                string result = notepadPlusMatch.Groups[1].Value;
                Logger.Log($"Chemin extrait avec pattern Notepad++: \"{result}\"", LogLevel.Debug);
                return result;
            }

            Logger.Log($"Impossible d'extraire un chemin de fichier depuis: \"{windowTitle}\"", LogLevel.Warning);
            return null;
        }

        /// <summary>
        /// Vérifie si le titre de la fenêtre indique un fichier modifié
        /// </summary>
        public bool IsModifiedTitle(string windowTitle)
        {
            // Si une expression régulière avec un groupe nommé "modified" est définie, l'utiliser
            if (!string.IsNullOrEmpty(_titlePattern))
            {
                try
                {
                    var match = Regex.Match(windowTitle, _titlePattern);
                    if (match.Success && match.Groups["modified"].Success)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignorer les erreurs d'expression régulière et continuer
                }
            }
            
            // Sinon, vérifier les indicateurs de modification
            foreach (var indicator in _modificationIndicators)
            {
                if (windowTitle.Contains(indicator))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
} 