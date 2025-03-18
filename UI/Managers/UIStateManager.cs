using System;
using System.Collections.Generic;

namespace AutosavePro.UI.Managers
{
    /// <summary>
    /// Gestionnaire d'état de l'interface utilisateur qui coordonne les interactions
    /// </summary>
    public class UIStateManager
    {
        private readonly BackupManager _backupManager;
        private readonly InstalledAppsManager _appsManager;
        
        // Délégués pour les actions de l'interface
        public delegate void UpdateUIDelegate();
        public delegate void UpdateApplicationsListDelegate();
        
        // Événements
        public event UpdateUIDelegate? UIUpdateRequested;
        public event UpdateApplicationsListDelegate? ApplicationsListUpdateRequested;
        
        // État de l'application
        public bool IsBackupEnabled { get; private set; }
        public int BackupIntervalMinutes { get; private set; }
        
        public UIStateManager(BackupManager backupManager, InstalledAppsManager appsManager)
        {
            _backupManager = backupManager;
            _appsManager = appsManager;
            
            // Initialiser les valeurs par défaut
            IsBackupEnabled = false;
            BackupIntervalMinutes = 5;
        }
        
        /// <summary>
        /// Active ou désactive la sauvegarde automatique
        /// </summary>
        public void SetBackupEnabled(bool enabled)
        {
            IsBackupEnabled = enabled;
            _backupManager.SetEnabled(enabled);
            
            Logger.Log($"Sauvegarde automatique {(enabled ? "activée" : "désactivée")}", LogLevel.Info);
            
            // Déclencher la mise à jour de l'interface
            UIUpdateRequested?.Invoke();
        }
        
        /// <summary>
        /// Définit l'intervalle de sauvegarde
        /// </summary>
        public void SetBackupInterval(int minutes)
        {
            BackupIntervalMinutes = minutes;
            _backupManager.SetBackupInterval(minutes);
            
            Logger.Log($"Intervalle de sauvegarde défini à {minutes} minutes", LogLevel.Info);
            
            // Déclencher la mise à jour de l'interface
            UIUpdateRequested?.Invoke();
        }
        
        /// <summary>
        /// Ajoute une application à surveiller
        /// </summary>
        public void AddApplication(InstalledApplication app)
        {
            if (app == null || string.IsNullOrEmpty(app.ExecutablePath))
                return;
                
            try
            {
                string watchPath = DetermineWatchPath(app);
                string processName = System.IO.Path.GetFileNameWithoutExtension(app.ExecutablePath);
                
                _backupManager.AddApplication(app.Name, processName, watchPath);
                Logger.Log($"Application ajoutée à la surveillance: {app.Name}, chemin: {watchPath}", LogLevel.Info);
                
                // Déclencher la mise à jour de la liste des applications
                ApplicationsListUpdateRequested?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de l'ajout de l'application {app.Name}: {ex.Message}", LogLevel.Error);
            }
        }
        
        /// <summary>
        /// Supprime une application de la surveillance
        /// </summary>
        public void RemoveApplication(string appName)
        {
            _backupManager.RemoveApplication(appName);
            Logger.Log($"Application retirée de la surveillance: {appName}", LogLevel.Info);
            
            // Déclencher la mise à jour de la liste des applications
            ApplicationsListUpdateRequested?.Invoke();
        }
        
        /// <summary>
        /// Détermine le chemin à surveiller selon le type d'application
        /// </summary>
        private string DetermineWatchPath(InstalledApplication app)
        {
            // Pour Office, surveiller le dossier Documents
            if (app.Type == ApplicationType.Office)
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            
            // Pour les autres apps, utiliser leur emplacement d'installation ou Documents
            if (!string.IsNullOrEmpty(app.InstallLocation) && System.IO.Directory.Exists(app.InstallLocation))
            {
                return app.InstallLocation;
            }
            
            // Utiliser le dossier Documents comme fallback
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
        
        /// <summary>
        /// Obtient la liste des applications installées
        /// </summary>
        public IEnumerable<InstalledApplication> GetInstalledApplications()
        {
            return _appsManager.GetInstalledApplications();
        }
        
        /// <summary>
        /// Obtient la liste des applications surveillées
        /// </summary>
        public IEnumerable<ApplicationWatcher> GetWatchedApplications()
        {
            return _backupManager.GetWatchedApplications();
        }
    }
} 