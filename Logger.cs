using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace AutosavePro
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public LogLevel Level { get; set; }

        public LogEntry(string message, LogLevel level)
        {
            Timestamp = DateTime.Now;
            Message = message;
            Level = level;
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level}] {Message}";
        }
    }

    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutosavePro",
            "Logs"
        );

        private static readonly object LogLock = new object();
        private static readonly List<LogEntry> RecentLogs = new List<LogEntry>();
        private static readonly int MaxRecentLogs = 1000;
        
        // Niveau de log minimal pour l'affichage dans l'interface et pour enregistrer dans le fichier
        private static LogLevel _minLogLevelConsole = LogLevel.Info;
        private static LogLevel _minLogLevelFile = LogLevel.Debug;

        public static event EventHandler<LogEntry>? OnNewLog;

        static Logger()
        {
            Directory.CreateDirectory(LogPath);
        }
        
        public static void SetMinLogLevel(LogLevel consoleLevel, LogLevel fileLevel)
        {
            _minLogLevelConsole = consoleLevel;
            _minLogLevelFile = fileLevel;
        }

        public static void Log(string message, LogLevel level = LogLevel.Info, string? module = null)
        {
            var entry = new LogEntry(message, level);
            
            // Ajouter le module au message si spécifié
            if (!string.IsNullOrEmpty(module))
            {
                entry.Message = $"[{module}] {entry.Message}";
            }
            
            string logFile = Path.Combine(LogPath, $"log_{DateTime.Now:yyyyMMdd}.txt");

            lock (LogLock)
            {
                try
                {
                    // Écrire dans le fichier si le niveau est suffisant
                    if (level >= _minLogLevelFile)
                    {
                        File.AppendAllText(logFile, entry.ToString() + Environment.NewLine);
                    }

                    // Ajouter aux logs récents et déclencher l'événement si le niveau est suffisant
                    if (level >= _minLogLevelConsole)
                    {
                        // Ajouter aux logs récents
                        RecentLogs.Add(entry);
                        if (RecentLogs.Count > MaxRecentLogs)
                        {
                            RecentLogs.RemoveAt(0);
                        }

                        // Déclencher l'événement
                        OnNewLog?.Invoke(null, entry);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur de journalisation : {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Message original : {message}");
                }
            }
        }

        public static List<LogEntry> GetRecentLogs(int count = 100)
        {
            lock (LogLock)
            {
                return RecentLogs.OrderByDescending(l => l.Timestamp)
                    .Take(count)
                    .OrderBy(l => l.Timestamp)
                    .ToList();
            }
        }

        public static void ClearRecentLogs()
        {
            lock (LogLock)
            {
                RecentLogs.Clear();
            }
        }
    }
} 