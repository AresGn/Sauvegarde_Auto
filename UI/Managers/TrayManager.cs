using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AutosavePro.UI.Managers
{
    /// <summary>
    /// Gestionnaire d'icône dans la barre d'état système et de fonctionnalités associées
    /// </summary>
    public class TrayManager : IDisposable
    {
        private readonly Form _ownerForm;
        private readonly UIStateManager _uiStateManager;
        private readonly NotifyIcon _notifyIcon;
        private bool _closeToTray = true;
        
        /// <summary>
        /// Indique si l'application doit se réduire dans la barre d'état système lors de la fermeture
        /// </summary>
        public bool CloseToTray
        {
            get => _closeToTray;
            set => _closeToTray = value;
        }
        
        public TrayManager(Form ownerForm, UIStateManager uiStateManager)
        {
            _ownerForm = ownerForm ?? throw new ArgumentNullException(nameof(ownerForm));
            _uiStateManager = uiStateManager ?? throw new ArgumentNullException(nameof(uiStateManager));
            
            // Initialisation de l'icône de barre d'état système
            _notifyIcon = new NotifyIcon
            {
                Visible = true,
                Text = "AutosavePro"
            };
            
            LoadTrayIcon();
            SetupContextMenu();
            RegisterFormEvents();
        }
        
        /// <summary>
        /// Charge l'icône pour la barre d'état système
        /// </summary>
        private void LoadTrayIcon()
        {
            try
            {
                if (File.Exists("autosave.ico"))
                {
                    _notifyIcon.Icon = new Icon("autosave.ico");
                }
                else if (File.Exists("AppIcon.ico"))
                {
                    _notifyIcon.Icon = new Icon("AppIcon.ico");
                }
                else if (_ownerForm.Icon != null)
                {
                    _notifyIcon.Icon = _ownerForm.Icon;
                }
                else
                {
                    // Utilisation d'une icône système par défaut
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors du chargement de l'icône pour la zone de notification: {ex.Message}", LogLevel.Warning);
                _notifyIcon.Icon = SystemIcons.Application;
            }
        }
        
        /// <summary>
        /// Configure le menu contextuel de l'icône
        /// </summary>
        private void SetupContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            
            var openItem = new ToolStripMenuItem("Ouvrir");
            openItem.Click += (s, e) => ShowApplication();
            
            var enableBackupItem = new ToolStripMenuItem("Activer la sauvegarde");
            enableBackupItem.Checked = true;
            enableBackupItem.Click += (s, e) => {
                enableBackupItem.Checked = !enableBackupItem.Checked;
                _uiStateManager.SetBackupEnabled(enableBackupItem.Checked);
            };
            
            var exitItem = new ToolStripMenuItem("Quitter");
            exitItem.Click += (s, e) => {
                _closeToTray = false;
                Application.Exit();
            };

            contextMenu.Items.AddRange(new ToolStripItem[] {
                openItem,
                new ToolStripSeparator(),
                enableBackupItem,
                new ToolStripSeparator(),
                exitItem
            });

            _notifyIcon.ContextMenuStrip = contextMenu;
            
            // Double-clic pour ouvrir l'application
            _notifyIcon.DoubleClick += (s, e) => ShowApplication();
        }
        
        /// <summary>
        /// Affiche l'application
        /// </summary>
        private void ShowApplication()
        {
            _ownerForm.Show();
            _ownerForm.WindowState = FormWindowState.Normal;
            _ownerForm.Activate();
        }
        
        /// <summary>
        /// Affiche une info-bulle dans la barre d'état système
        /// </summary>
        public void ShowBalloonTip(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000)
        {
            _notifyIcon.ShowBalloonTip(timeout, title, message, icon);
        }
        
        /// <summary>
        /// Enregistre les événements du formulaire
        /// </summary>
        private void RegisterFormEvents()
        {
            // Gérer la fermeture du formulaire
            _ownerForm.FormClosing += (s, e) => {
                if (_closeToTray && e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    _ownerForm.Hide();
                    ShowBalloonTip(
                        "AutoSavePro",
                        "L'application continue à fonctionner en arrière-plan. Double-cliquez sur l'icône pour la réouvrir.");
                }
            };
            
            // Gérer le changement de taille
            _ownerForm.SizeChanged += (s, e) => {
                if (_ownerForm.WindowState == FormWindowState.Minimized)
                {
                    _ownerForm.Hide();
                    ShowBalloonTip(
                        "AutoSavePro",
                        "L'application a été minimisée dans la zone de notification.");
                }
            };
        }
        
        /// <summary>
        /// Libère les ressources utilisées par le gestionnaire de barre d'état système
        /// </summary>
        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
} 