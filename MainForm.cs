using System;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using AutosavePro.Helpers;
using AutosavePro.UI.Controls;
using AutosavePro.UI.Managers;
using AutosavePro.Models;

namespace AutosavePro
{
    public partial class MainForm : Form
    {
        // Managers
        private BackupManager _backupManager = null!;
        private UIStateManager _uiStateManager = null!;
        private IconManager _iconManager = null!;
        private FormConfigManager _formConfigManager = null!;
        private TabControlManager _tabControlManager = null!;
        private TrayManager _trayManager = null!;
        private SplitterManager _splitterManager = null!;
        
        // Contrôles et timers
        private System.Windows.Forms.Timer _uiUpdateTimer = null!;
        private readonly InstalledAppsManager _appsManager = new();
        private ApplicationListControl _availableAppsControl = null!;
        private ApplicationListControl _watchedAppsControl = null!;
        private LogViewerControl _logViewerControl = null!;
        private SplitContainer _mainSplitContainer = null!;
        private System.Windows.Forms.Timer? _sizeChangeTimer = null;
        private NotifyIcon _notifyIcon = null!;
        private bool _closeToTray = true;

        public MainForm()
        {
            // Créer les managers de base
            _iconManager = new IconManager();
            _appsManager = new InstalledAppsManager();
            
            // Initialiser le BackupManager et UIStateManager en premier
            InitializeBackupManager();
            
            // Initialiser TrayManager après UIStateManager mais avant les composants
            InitializeNotifyIcon();
            
            // Initialiser les composants de l'interface
            InitializeComponent();
            
            // Configurer les contrôles après initialisation complète
            SetupUIUpdateTimer();
            
            // Ajouter un gestionnaire pour le redimensionnement
            this.SizeChanged += MainForm_SizeChanged;
        }

        private void InitializeComponent()
        {
            // Initialiser le FormConfigManager avant tout
            _formConfigManager = new FormConfigManager(this);
            _formConfigManager.ConfigureForm();
            var mainMenu = _formConfigManager.CreateMainMenu(SettingsMenu_Click, ConfigAppsMenu_Click);
            this.MainMenuStrip = mainMenu;
            this.Controls.Add(mainMenu);
            
            // Vérifier que UIStateManager est bien initialisé
            if (_uiStateManager == null)
            {
                throw new InvalidOperationException("UIStateManager n'est pas initialisé. Assurez-vous que InitializeBackupManager() est appelé avant InitializeComponent().");
            }
            
            // Initialiser TabControlManager après que UIStateManager soit initialisé
            _tabControlManager = new TabControlManager(this, _iconManager, _uiStateManager);
            
            // Créer le TabControl et l'attacher au formulaire
            var mainTabControl = _tabControlManager.CreateTabControl();
            this.Controls.Add(mainTabControl);
            
            // Récupérer les contrôles créés par le TabControlManager
            _mainSplitContainer = _tabControlManager.MainSplitContainer;
            _availableAppsControl = _tabControlManager.AvailableAppsControl;
            _watchedAppsControl = _tabControlManager.WatchedAppsControl;
            _logViewerControl = _tabControlManager.LogViewerControl;
            
            // Initialiser le SplitterManager
            _splitterManager = new SplitterManager(_mainSplitContainer, this);
            
            // Configurer les handlers d'événements
            AttachEventHandlers();
            
            // Charger les applications installées
            LoadInstalledApplications();
        }

        private void AttachEventHandlers()
        {
            // Attacher les gestionnaires pour les éléments de menu
            if (MainMenuStrip != null && MainMenuStrip.Items.Count > 0)
            {
                var fileMenu = MainMenuStrip.Items[0] as ToolStripMenuItem;
                if (fileMenu != null && fileMenu.DropDownItems.Count >= 4)
                {
                    var settingsMenu = fileMenu.DropDownItems[0] as ToolStripMenuItem;
                    var configAppsMenu = fileMenu.DropDownItems[1] as ToolStripMenuItem;
                    var exitMenu = fileMenu.DropDownItems[3] as ToolStripMenuItem;
                    
                    if (settingsMenu != null)
                        settingsMenu.Click += SettingsMenu_Click;
                    
                    if (configAppsMenu != null)
                        configAppsMenu.Click += ConfigAppsMenu_Click;
                    
                    if (exitMenu != null)
                        exitMenu.Click += (s, e) => this.Close();
                }
            }
            
            // Attacher les gestionnaires pour les boutons d'application
            var addAppButton = _tabControlManager.AddApplicationButton;
            var removeAppButton = _tabControlManager.RemoveApplicationButton;
            var refreshAppsButton = _tabControlManager.RefreshApplicationsButton;
            
            if (addAppButton != null)
                addAppButton.Click += (s, e) => AddSelectedApplications();
            
            if (removeAppButton != null)
                removeAppButton.Click += (s, e) => RemoveSelectedApplications();
            
            if (refreshAppsButton != null)
                refreshAppsButton.Click += (s, e) => LoadInstalledApplications();
            
            // Attacher les gestionnaires pour les contrôles d'intervalle
            var intervalComboBox = _tabControlManager.IntervalComboBox;
            if (intervalComboBox != null)
            {
                intervalComboBox.SelectedIndexChanged += (s, e) => {
                    if (intervalComboBox.SelectedItem is SaveInterval selectedInterval)
                    {
                        _uiStateManager.SetBackupInterval(selectedInterval.Value);
                    }
                };
            }
            
            // Attacher le gestionnaire pour le toggle de sauvegarde
            var enableBackupToggle = _tabControlManager.EnableBackupToggle;
            if (enableBackupToggle != null)
            {
                enableBackupToggle.CheckedChanged += (s, e) => {
                    enableBackupToggle.Text = enableBackupToggle.Checked ? "ON" : "OFF";
                    enableBackupToggle.BackColor = enableBackupToggle.Checked ? 
                        Color.FromArgb(76, 175, 80) : Color.FromArgb(200, 200, 200);
                    _uiStateManager.SetBackupEnabled(enableBackupToggle.Checked);
                };
            }
            
            // Attacher le gestionnaire pour le bouton d'arrêt
            var stopButton = _tabControlManager.StopButton;
            if (stopButton != null)
            {
                stopButton.Click += (s, e) => {
                    if (MessageBox.Show(
                        "Voulez-vous vraiment arrêter l'application ?",
                        "Confirmation",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _backupManager.Dispose();
                        Application.Exit();
                    }
                };
            }
            
            // Configurer les gestionnaires de SplitContainer
            if (_mainSplitContainer != null)
            {
                _mainSplitContainer.SplitterMoved += (sender, args) => {
                    System.Windows.Forms.Timer checkTimer = new System.Windows.Forms.Timer();
                    checkTimer.Interval = 50;
                    checkTimer.Tick += (s, e) => {
                        checkTimer.Stop();
                        checkTimer.Dispose();
                        EnsureSafeSplitterDistance();
                    };
                    checkTimer.Start();
                };
            }
        }

        private void InitializeBackupManager()
        {
            string backupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "AutosavePro"
            );

            // Créer d'abord BackupManager
            _backupManager = new BackupManager(backupPath);
            
            // Puis créer UIStateManager avec les dépendances
            _uiStateManager = new UIStateManager(_backupManager, _appsManager);
            
            // Vérification que _uiStateManager est bien initialisé
            if (_uiStateManager == null)
            {
                throw new InvalidOperationException("Échec de l'initialisation de UIStateManager");
            }
            
            // S'abonner aux événements du UIStateManager
            _uiStateManager.ApplicationsListUpdateRequested += () => UpdateApplicationsList();
            
            // Configurer BackupManager
            _backupManager.SetBackupInterval(5); // Par défaut 5 minutes
            _backupManager.SetEnabled(false); // Désactivé par défaut

            // Ajouter les applications par défaut
            var defaultApps = new Dictionary<string, (string ProcessName, string WatchPath)>
            {
                { "Microsoft Word", ("WINWORD", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)) },
                { "Microsoft Excel", ("EXCEL", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)) },
                { "Microsoft PowerPoint", ("POWERPNT", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)) },
                { "Microsoft OneNote", ("ONENOTE", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)) },
                { "Bloc-notes", ("notepad", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)) },
                { "Visual Studio Code", ("Code", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)) }
            };
            
            foreach (var app in defaultApps)
            {
                try
                {
                    _backupManager.AddApplication(app.Key, app.Value.ProcessName, app.Value.WatchPath);
                    Logger.Log($"Application par défaut ajoutée: {app.Key}", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Erreur lors de l'ajout de {app.Key}: {ex.Message}", LogLevel.Error);
                }
            }
            
            Logger.Log("BackupManager initialisé avec les applications par défaut");
            
            // Mettre à jour la liste des applications surveillées
            UpdateApplicationsList();
        }

        private void SetupUIUpdateTimer()
        {
            _uiUpdateTimer = new System.Windows.Forms.Timer();
            _uiUpdateTimer.Interval = 1000; // Mise à jour toutes les secondes
            _uiUpdateTimer.Tick += UpdateUI;
            _uiUpdateTimer.Start();
        }

        private void UpdateUI(object? sender, EventArgs e)
        {
            // Mise à jour des logs - déléguée au LogViewerControl
            _logViewerControl?.RefreshLogs();
        }

        private void LoadInstalledApplications()
        {
            var apps = _appsManager.GetInstalledApplications()
                .OrderBy(a => a.Type)
                .ThenBy(a => a.Name)
                .ToList();

            _availableAppsControl.ClearItems();
            
            foreach (var app in apps)
            {
                int imageIndex = _iconManager.GetIconIndex(app);
                
                var item = new ListViewItem(new string[] { 
                    "", 
                    app.Name,
                    app.Publisher ?? "Inconnu",
                    app.Type.ToString()
                }, imageIndex);
                
                item.Tag = app;
                _availableAppsControl.AddItem(item);
            }

            Logger.Log($"{apps.Count} applications trouvées");
        }

        private void AddSelectedApplications()
        {
            var selectedItems = _availableAppsControl.ListView.SelectedItems;
            
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner au moins une application", "Aucune application sélectionnée",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (ListViewItem item in selectedItems)
            {
                try
                {
                    if (item.Tag is not InstalledApplication app || string.IsNullOrEmpty(app.ExecutablePath))
                        continue;

                    _uiStateManager.AddApplication(app);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Erreur lors de l'ajout de l'application: {ex.Message}", LogLevel.Error);
                }
            }
        }

        private void RemoveSelectedApplications()
        {
            var selectedItems = _watchedAppsControl.ListView.SelectedItems;
            
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner une application à retirer.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedApps = selectedItems.Cast<ListViewItem>()
                .Select(item => item.SubItems[1].Text)
                .ToList();

            foreach (var appName in selectedApps)
            {
                _uiStateManager.RemoveApplication(appName);
            }

            Logger.Log($"{selectedApps.Count} application(s) retirée(s) de la surveillance");
        }

        private void UpdateApplicationsList()
        {
            _watchedAppsControl.ClearItems();
            
            foreach (var app in _backupManager.GetWatchedApplications())
            {
                string baseAppName = app.AppName
                    .Replace(" (Documents)", "")
                    .Replace(" (Bureau)", "");
                
                int imageIndex = _iconManager.GetIconIndexForWatchedApp(app);
                
                var item = new ListViewItem(new string[] {
                    "",
                    app.AppName,
                    app.ProcessName,
                    app.WatchPath
                }, imageIndex);
                
                _watchedAppsControl.AddItem(item);
            }
        }

        private void SettingsMenu_Click(object? sender, EventArgs e)
        {
            // TODO: Implémenter la fenêtre des paramètres
            MessageBox.Show("Paramètres à implémenter", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ConfigAppsMenu_Click(object? sender, EventArgs e)
        {
            try
            {
                // Créer une nouvelle configuration pour tester
                var config = new ApplicationConfig("Application test", "test");
                using (var configForm = new ApplicationConfigForm(config, true))
                {
                    if (configForm.ShowDialog(this) == DialogResult.OK)
                    {
                        MessageBox.Show($"Configuration enregistrée pour {config.AppName}", 
                            "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de l'ouverture de la configuration: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // Configurer les événements du formulaire via le FormConfigManager
            _formConfigManager.ConfigureLoadEvent(
                UpdateApplicationsList,
                EnsureSafeSplitterDistance,
                () => ApplicationRegistry.Instance.ShouldShowWelcomeDialog()
            );
            
            // Mettre à jour la liste des applications, le BackupManager étant déjà initialisé
            UpdateApplicationsList();
            
            // Ajouter un dernier filet de sécurité après le chargement complet
            this.BeginInvoke(new Action(() => EnsureSafeSplitterDistance()));
        }

        // Méthode utilitaire pour s'assurer que le SplitterDistance est toujours valide
        private void EnsureSafeSplitterDistance()
        {
            _splitterManager.EnsureSafeSplitterDistance();
        }

        private void InitializeNotifyIcon()
        {
            // Utiliser le TrayManager pour gérer la zone de notification
            _trayManager = new TrayManager(this, _uiStateManager);
            _closeToTray = true; // Définir la propriété du form
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_closeToTray && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                _trayManager.ShowBalloonTip(
                    "AutoSavePro",
                    "L'application continue à fonctionner en arrière-plan. Double-cliquez sur l'icône pour la réouvrir."
                );
            }
            else
            {
                base.OnFormClosing(e);
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                _trayManager.ShowBalloonTip(
                    "AutoSavePro",
                    "L'application a été minimisée dans la zone de notification."
                );
            }
        }

        private void MainForm_SizeChanged(object? sender, EventArgs e)
        {
            // Ne pas redéfinir le SplitterDistance à chaque redimensionnement
            // pour éviter de déclencher des erreurs
            if (_sizeChangeTimer == null)
            {
                _sizeChangeTimer = new System.Windows.Forms.Timer
                {
                    Interval = 200  // Augmenté pour plus de stabilité
                };
                
                _sizeChangeTimer.Tick += (s, ev) => {
                    _sizeChangeTimer?.Stop();
                    EnsureSafeSplitterDistance();
                };
            }
            
            // Restart the timer
            _sizeChangeTimer.Stop();
            _sizeChangeTimer.Start();
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _uiUpdateTimer?.Dispose();
                _backupManager?.Dispose();
                _trayManager?.Dispose();
                _splitterManager?.Dispose();
                _sizeChangeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 