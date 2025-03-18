using System;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace AutosavePro
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Configuration du gestionnaire d'exceptions non gérées
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            
            try
            {
                // Création du dossier de logs si nécessaire
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string logPath = Path.Combine(appDataPath, "AutosavePro", "Logs");
                Directory.CreateDirectory(logPath);
                
                // Écrire un message de démarrage dans un fichier texte simple
                File.AppendAllText(
                    Path.Combine(logPath, "startup.log"), 
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Application démarrée{Environment.NewLine}"
                );
                
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                // En cas d'erreur critique au démarrage, écrire dans un fichier
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string logPath = Path.Combine(appDataPath, "AutosavePro", "Logs");
                Directory.CreateDirectory(logPath);
                
                File.AppendAllText(
                    Path.Combine(logPath, "critical_error.log"), 
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERREUR CRITIQUE: {ex.Message}{Environment.NewLine}Stack: {ex.StackTrace}{Environment.NewLine}"
                );
                
                MessageBox.Show($"Une erreur critique est survenue: {ex.Message}", "Erreur", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogUnhandledException("Thread Exception", e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogUnhandledException("AppDomain Exception", ex);
            }
            else if (e.ExceptionObject != null)
            {
                LogUnhandledException("AppDomain Exception", new Exception($"Erreur non-standard: {e.ExceptionObject}"));
            }
            else
            {
                LogUnhandledException("AppDomain Exception", new Exception("Erreur inconnue"));
            }
        }

        private static void LogUnhandledException(string type, Exception ex)
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string logPath = Path.Combine(appDataPath, "AutosavePro", "Logs");
                Directory.CreateDirectory(logPath);
                
                File.AppendAllText(
                    Path.Combine(logPath, "unhandled_exceptions.log"), 
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {type}: {ex?.Message}{Environment.NewLine}" +
                    $"Stack: {ex?.StackTrace}{Environment.NewLine}{Environment.NewLine}"
                );
                
                MessageBox.Show($"Une erreur non gérée est survenue: {ex?.Message}", "Erreur", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // Ne rien faire en cas d'échec de la journalisation
            }
        }
    }
} 