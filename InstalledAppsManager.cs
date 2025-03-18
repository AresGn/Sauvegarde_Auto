using System;
using System.Collections.Generic;
using System.Management;
using Microsoft.Win32;
using System.Linq;
using System.Drawing;
using System.IO;

namespace AutosavePro
{
    public class InstalledApplication
    {
        public string Name { get; set; }
        public string ExecutablePath { get; set; }
        public string? InstallLocation { get; set; }
        public string? Publisher { get; set; }
        public Icon? AppIcon { get; set; }
        public ApplicationType Type { get; set; }

        public InstalledApplication(string name, string execPath, string? installLocation, string? publisher, ApplicationType type)
        {
            Name = name;
            ExecutablePath = execPath;
            InstallLocation = installLocation;
            Publisher = publisher;
            Type = type;
            try
            {
                if (!string.IsNullOrEmpty(execPath) && File.Exists(execPath))
                {
                    AppIcon = Icon.ExtractAssociatedIcon(execPath);
                }
            }
            catch
            {
                AppIcon = null;
            }
        }
    }

    public enum ApplicationType
    {
        General,
        Office,
        Browser,
        MediaPlayer,
        Antivirus
    }

    public class InstalledAppsManager
    {
        private static readonly string[] RegistryKeys = new string[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\Microsoft\Office\ClickToRun\REGISTRY\MACHINE\Software\Microsoft\Office\ClickToRun\Configuration",
            @"SOFTWARE\Microsoft\Office\ClickToRun\Configuration"
        };

        private static readonly string[] ExcludedKeywords = new string[]
        {
            "redistributable", "sdk", "runtime", "driver", "update", "installer",
            "language pack", "helper", "microsoft visual c++", ".net", "framework"
        };

        private static readonly string[] BrowserKeywords = new string[]
        {
            "chrome", "firefox", "edge", "opera", "browser", "internet explorer", 
            "safari", "brave", "chromium", "mozilla"
        };

        private static readonly string[] MediaPlayerKeywords = new string[]
        {
            "vlc", "media player", "winamp", "foobar", "mpc-hc", "potplayer", 
            "gom player", "windows media player"
        };

        private static readonly string[] AntivirusKeywords = new string[]
        {
            "antivirus", "security", "defender", "protection", "mcafee", 
            "norton", "avast", "avg", "kaspersky", "bitdefender"
        };

        public List<InstalledApplication> GetInstalledApplications()
        {
            var applications = new HashSet<InstalledApplication>(new InstalledApplicationComparer());

            // Recherche des applications Windows intégrées
            FindWindowsApps(applications);

            // Recherche des applications Office
            FindOfficeApps(applications);

            // Recherche dans le registre
            foreach (string registryKey in RegistryKeys)
            {
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        foreach (string subkeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey? subkey = key.OpenSubKey(subkeyName))
                            {
                                if (subkey == null) continue;

                                try
                                {
                                    string? displayName = subkey.GetValue("DisplayName") as string;
                                    if (string.IsNullOrEmpty(displayName)) continue;

                                    // Vérifier si l'application doit être exclue
                                    if (ShouldExcludeApplication(displayName))
                                        continue;

                                    string? installLocation = subkey.GetValue("InstallLocation") as string;
                                    string? publisher = subkey.GetValue("Publisher") as string;
                                    string? exePath = subkey.GetValue("DisplayIcon") as string;

                                    // Déterminer le type d'application
                                    var appType = DetermineApplicationType(displayName, exePath);
                                    
                                    // Ignorer les navigateurs et autres applications exclues
                                    if (appType == ApplicationType.Browser)
                                        continue;

                                    // Nettoyer le chemin de l'exécutable
                                    if (!string.IsNullOrEmpty(exePath))
                                    {
                                        exePath = CleanExecutablePath(exePath);
                                    }

                                    // Si pas de chemin d'installation, essayer de le trouver à partir de l'exécutable
                                    if (string.IsNullOrEmpty(installLocation) && !string.IsNullOrEmpty(exePath))
                                    {
                                        installLocation = Path.GetDirectoryName(exePath);
                                    }

                                    if (!string.IsNullOrEmpty(exePath))
                                    {
                                        applications.Add(new InstalledApplication(
                                            displayName,
                                            exePath,
                                            installLocation,
                                            publisher,
                                            appType
                                        ));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log($"Erreur lors de la lecture du registre : {ex.Message}", LogLevel.Error);
                                }
                            }
                        }
                    }
                }
            }

            return applications.Where(app => !string.IsNullOrEmpty(app.ExecutablePath)).ToList();
        }

        private void FindWindowsApps(HashSet<InstalledApplication> applications)
        {
            try
            {
                // Ajouter le Bloc-notes
                string notepadPath = Path.Combine(Environment.SystemDirectory, "notepad.exe");
                if (File.Exists(notepadPath))
                {
                    applications.Add(new InstalledApplication(
                        "Bloc-notes",
                        notepadPath,
                        Environment.SystemDirectory,
                        "Microsoft Corporation",
                        ApplicationType.General
                    ));
                }

                // Ajouter WordPad
                string wordpadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                    "Windows NT\\Accessories\\wordpad.exe");
                if (File.Exists(wordpadPath))
                {
                    applications.Add(new InstalledApplication(
                        "WordPad",
                        wordpadPath,
                        Path.GetDirectoryName(wordpadPath),
                        "Microsoft Corporation",
                        ApplicationType.General
                    ));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la recherche des applications Windows : {ex.Message}", LogLevel.Error);
            }
        }

        private void FindOfficeApps(HashSet<InstalledApplication> applications)
        {
            var officePaths = new[]
            {
                @"C:\Program Files\Microsoft Office\root\Office16",
                @"C:\Program Files (x86)\Microsoft Office\root\Office16",
                @"C:\Program Files\Microsoft Office\Office16",
                @"C:\Program Files (x86)\Microsoft Office\Office16"
            };

            var officeApps = new Dictionary<string, string>
            {
                { "WINWORD.EXE", "Microsoft Word" },
                { "EXCEL.EXE", "Microsoft Excel" },
                { "POWERPNT.EXE", "Microsoft PowerPoint" },
                { "OUTLOOK.EXE", "Microsoft Outlook" },
                { "ONENOTE.EXE", "Microsoft OneNote" },
                { "MSACCESS.EXE", "Microsoft Access" },
                { "MSPUB.EXE", "Microsoft Publisher" }
            };

            try
            {
                foreach (string officePath in officePaths)
                {
                    if (Directory.Exists(officePath))
                    {
                        foreach (var officeApp in officeApps)
                        {
                            string exePath = Path.Combine(officePath, officeApp.Key);
                            if (File.Exists(exePath))
                            {
                                applications.Add(new InstalledApplication(
                                    officeApp.Value,
                                    exePath,
                                    officePath,
                                    "Microsoft Corporation",
                                    ApplicationType.Office
                                ));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la recherche des applications Office : {ex.Message}", LogLevel.Error);
            }
        }

        private bool ShouldExcludeApplication(string name)
        {
            string nameLower = name.ToLower();
            return ExcludedKeywords.Any(keyword => nameLower.Contains(keyword.ToLower()));
        }

        private ApplicationType DetermineApplicationType(string name, string? exePath)
        {
            string nameLower = name.ToLower();

            if (BrowserKeywords.Any(k => nameLower.Contains(k)))
                return ApplicationType.Browser;

            if (MediaPlayerKeywords.Any(k => nameLower.Contains(k)))
                return ApplicationType.MediaPlayer;

            if (AntivirusKeywords.Any(k => nameLower.Contains(k)))
                return ApplicationType.Antivirus;

            if (nameLower.Contains("office") || nameLower.Contains("word") || 
                nameLower.Contains("excel") || nameLower.Contains("powerpoint"))
                return ApplicationType.Office;

            return ApplicationType.General;
        }

        private string? CleanExecutablePath(string? path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Supprimer les paramètres de ligne de commande
            path = path.Split(',')[0].Trim('"', ' ');

            if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                // Essayer de trouver l'exécutable dans le même dossier
                string? directory = Path.GetDirectoryName(path);
                if (directory != null && Directory.Exists(directory))
                {
                    string possibleExe = Path.Combine(directory, Path.GetFileNameWithoutExtension(path) + ".exe");
                    if (File.Exists(possibleExe))
                    {
                        return possibleExe;
                    }
                }
                return null;
            }

            return path;
        }

        private class InstalledApplicationComparer : IEqualityComparer<InstalledApplication>
        {
            public bool Equals(InstalledApplication? x, InstalledApplication? y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;
                return x.Name.Equals(y.Name, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(InstalledApplication obj)
            {
                return obj.Name.ToLower().GetHashCode();
            }
        }
    }
} 