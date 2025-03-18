using System;
using System.Drawing;
using System.Windows.Forms;
using AutosavePro.Helpers;
using AutosavePro.Models;
using AutosavePro.UI.Controls;

namespace AutosavePro.UI.Managers
{
    /// <summary>
    /// Gestionnaire pour les onglets et leur contenu
    /// </summary>
    public class TabControlManager
    {
        private readonly Form _ownerForm;
        private readonly IconManager _iconManager;
        private readonly UIStateManager _uiStateManager;
        private TabControl _mainTabControl;
        
        private ApplicationListControl _availableAppsControl;
        private ApplicationListControl _watchedAppsControl;
        private LogViewerControl _logViewerControl;
        private SplitContainer _mainSplitContainer;
        private SplitterManager _splitterManager;
        
        // Contrôles d'interaction
        private Button _addApplicationButton;
        private Button _removeApplicationButton;
        private Button _refreshApplicationsButton;
        private Button _stopButton;
        private ComboBox _intervalComboBox;
        private CheckBox _enableBackupToggle;
        
        // Propriétés publiques pour les contrôles
        public ApplicationListControl AvailableAppsControl => _availableAppsControl;
        public ApplicationListControl WatchedAppsControl => _watchedAppsControl;
        public LogViewerControl LogViewerControl => _logViewerControl;
        public SplitContainer MainSplitContainer => _mainSplitContainer;
        public SplitterManager SplitterManager => _splitterManager;
        
        // Propriétés pour les contrôles d'interaction
        public Button AddApplicationButton => _addApplicationButton;
        public Button RemoveApplicationButton => _removeApplicationButton;
        public Button RefreshApplicationsButton => _refreshApplicationsButton;
        public Button StopButton => _stopButton;
        public ComboBox IntervalComboBox => _intervalComboBox;
        public CheckBox EnableBackupToggle => _enableBackupToggle;
        
        public TabControlManager(Form ownerForm, IconManager iconManager, UIStateManager uiStateManager)
        {
            _ownerForm = ownerForm ?? throw new ArgumentNullException(nameof(ownerForm));
            _iconManager = iconManager ?? throw new ArgumentNullException(nameof(iconManager));
            _uiStateManager = uiStateManager ?? throw new ArgumentNullException(nameof(uiStateManager));
        }
        
        /// <summary>
        /// Crée et configure le TabControl et ses onglets
        /// </summary>
        public TabControl CreateTabControl()
        {
            _mainTabControl = UIHelpers.CreateModernTabControl();
            
            // Créer les onglets
            TabPage applicationsTab = CreateApplicationsTab();
            TabPage logsTab = CreateLogsTab();
            
            // Ajouter les onglets
            _mainTabControl.TabPages.Add(applicationsTab);
            _mainTabControl.TabPages.Add(logsTab);
            
            return _mainTabControl;
        }
        
        /// <summary>
        /// Crée l'onglet Applications
        /// </summary>
        private TabPage CreateApplicationsTab()
        {
            TabPage applicationsTab = new TabPage("Applications");
            applicationsTab.Padding = new Padding(15);
            
            // Panel principal avec layout vertical
            TableLayoutPanel mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                RowStyles = {
                    new RowStyle(SizeType.Absolute, 60),
                    new RowStyle(SizeType.Percent, 100)
                },
                Padding = new Padding(0, 0, 0, 10),
                BackColor = Color.White
            };
            
            // Créer le panel de contrôle en haut
            TableLayoutPanel controlPanel = CreateControlPanel();
            
            // Split container pour diviser la vue
            _mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = Color.White
            };
            
            // Gestionnaire pour le splitter
            _splitterManager = new SplitterManager(_mainSplitContainer, _ownerForm);
            
            // Créer et configurer les contrôles de liste d'applications
            _availableAppsControl = new ApplicationListControl(_iconManager)
            {
                Dock = DockStyle.Fill,
                Title = "Applications disponibles"
            };
            
            _watchedAppsControl = new ApplicationListControl(_iconManager)
            {
                Dock = DockStyle.Fill,
                Title = "Applications surveillées"
            };
            
            // Configuration des colonnes pour chaque contrôle
            _availableAppsControl.ConfigureColumns(new[]
            {
                new ColumnHeader { Text = "", Width = 30 },
                new ColumnHeader { Text = "Nom", Width = 270 },
                new ColumnHeader { Text = "Éditeur", Width = 120 },
                new ColumnHeader { Text = "Type", Width = 80 }
            });
            
            _watchedAppsControl.ConfigureColumns(new[]
            {
                new ColumnHeader { Text = "", Width = 30 },
                new ColumnHeader { Text = "Nom", Width = 150 },
                new ColumnHeader { Text = "Processus", Width = 110 },
                new ColumnHeader { Text = "Dossier surveillé", Width = 240 }
            });
            
            // Attacher les contrôles aux panels
            _mainSplitContainer.Panel1.Controls.Add(_availableAppsControl);
            _mainSplitContainer.Panel2.Controls.Add(_watchedAppsControl);
            
            // Boutons pour l'onglet Applications
            FlowLayoutPanel applicationButtonsPanel = CreateApplicationButtonsPanel();
            
            // Assemblage de l'interface de l'onglet Applications
            mainPanel.Controls.Add(controlPanel, 0, 0);
            mainPanel.Controls.Add(_mainSplitContainer, 0, 1);
            
            applicationsTab.Controls.Add(mainPanel);
            applicationsTab.Controls.Add(applicationButtonsPanel);
            
            return applicationsTab;
        }
        
        /// <summary>
        /// Crée le panel de contrôle pour l'onglet Applications
        /// </summary>
        private TableLayoutPanel CreateControlPanel()
        {
            TableLayoutPanel controlPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 60,
                ColumnCount = 4,
                ColumnStyles = {
                    new ColumnStyle(SizeType.AutoSize),  // Pour intervalLabel
                    new ColumnStyle(SizeType.AutoSize),  // Pour intervalComboBox
                    new ColumnStyle(SizeType.Percent, 100),  // Pour enableBackupToggle
                    new ColumnStyle(SizeType.AutoSize)   // Pour stopButton
                },
                BackColor = Color.White
            };
            
            // Contrôles pour l'intervalle de sauvegarde
            Label intervalLabel = new Label
            {
                Text = "Intervalle:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(50, 50, 50),
                Padding = new Padding(0, 4, 0, 0)
            };
            
            _intervalComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120,
                Font = new Font("Segoe UI", 10f),
                FlatStyle = FlatStyle.Flat
            };
            
            // Ajouter les intervalles disponibles
            var intervals = new[] 
            {
                new SaveInterval("1 minute", 1),
                new SaveInterval("2 minutes", 2),
                new SaveInterval("5 minutes", 5),
                new SaveInterval("10 minutes", 10),
                new SaveInterval("25 minutes", 25)
            };
            
            _intervalComboBox.Items.AddRange(intervals);
            _intervalComboBox.SelectedIndex = 2; // 5 minutes par défaut
            
            _enableBackupToggle = new CheckBox
            {
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                Checked = false,
                Size = new Size(80, 30),
                Margin = new Padding(10, 0, 5, 0),
                Text = "OFF",
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            
            // Construction du panel pour contenir le toggle et son libellé
            TableLayoutPanel togglePanel = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            
            // Label avant le toggle (pour qu'il soit à gauche)
            Label toggleLabel = new Label
            {
                Text = "Sauvegarde automatique:",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 4, 5, 0)
            };
            
            // Configurer la mise en page du panel
            togglePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Pour le label
            togglePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Pour le toggle
            
            // Ajouter les contrôles dans l'ordre: label puis toggle
            togglePanel.Controls.Add(toggleLabel, 0, 0);
            togglePanel.Controls.Add(_enableBackupToggle, 1, 0);
            
            // Bouton d'arrêt - couleur plus vive
            _stopButton = UIHelpers.CreateModernButton("Arrêter", Color.FromArgb(231, 76, 60));
            
            // Ajout des contrôles au panel de contrôle
            controlPanel.Controls.Add(intervalLabel, 0, 0);
            controlPanel.Controls.Add(_intervalComboBox, 1, 0);
            controlPanel.Controls.Add(togglePanel, 2, 0);
            controlPanel.Controls.Add(_stopButton, 3, 0);
            
            return controlPanel;
        }
        
        /// <summary>
        /// Crée le panel de boutons pour les applications
        /// </summary>
        private FlowLayoutPanel CreateApplicationButtonsPanel()
        {
            FlowLayoutPanel applicationButtonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                Padding = new Padding(5),
                BackColor = Color.White
            };
            
            _addApplicationButton = UIHelpers.CreateModernButton("Ajouter", Color.FromArgb(76, 175, 80));
            _removeApplicationButton = UIHelpers.CreateModernButton("Retirer", Color.FromArgb(239, 83, 80));
            _refreshApplicationsButton = UIHelpers.CreateModernButton("Rafraîchir", Color.FromArgb(85, 85, 85));
            
            applicationButtonsPanel.Controls.AddRange(new Control[] { 
                _addApplicationButton, _removeApplicationButton, _refreshApplicationsButton 
            });
            
            return applicationButtonsPanel;
        }
        
        /// <summary>
        /// Crée l'onglet Logs
        /// </summary>
        private TabPage CreateLogsTab()
        {
            TabPage logsTab = new TabPage("Journaux");
            logsTab.Padding = new Padding(15);
            
            // Créer le contrôle de visualisation des logs
            _logViewerControl = new LogViewerControl
            {
                Dock = DockStyle.Fill
            };
            
            logsTab.Controls.Add(_logViewerControl);
            
            return logsTab;
        }
        
        // Événements pour les boutons
        public event EventHandler OnAddButtonClicked;
        public event EventHandler OnRemoveButtonClicked;
        public event EventHandler OnRefreshButtonClicked;
    }
} 