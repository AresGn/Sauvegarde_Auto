using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AutosavePro.Helpers;

namespace AutosavePro.UI.Managers
{
    /// <summary>
    /// Gestionnaire pour la configuration du formulaire principal
    /// </summary>
    public class FormConfigManager
    {
        private readonly Form _form;
        
        public FormConfigManager(Form form)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
        }
        
        /// <summary>
        /// Configure les propriétés de base du formulaire
        /// </summary>
        public void ConfigureForm()
        {
            // Configuration de base
            _form.Text = "AutosavePro";
            _form.Size = new Size(1000, 650);
            _form.MinimumSize = new Size(700, 600);
            _form.FormBorderStyle = FormBorderStyle.Sizable;
            _form.MaximizeBox = true;
            _form.StartPosition = FormStartPosition.CenterScreen;
            _form.BackColor = Color.White;
            
            // Gestion de l'icône
            LoadIcon();
        }
        
        /// <summary>
        /// Charge l'icône du formulaire
        /// </summary>
        private void LoadIcon()
        {
            try
            {
                if (File.Exists("autosave.ico"))
                {
                    _form.Icon = new Icon("autosave.ico");
                }
                else if (File.Exists("AppIcon.ico"))
                {
                    _form.Icon = new Icon("AppIcon.ico");
                }
                else
                {
                    // Utilisation d'une icône depuis les ressources système
                    _form.Icon = SystemIcons.Application;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors du chargement de l'icône: {ex.Message}", LogLevel.Warning);
                // L'application continuera sans icône personnalisée
                _form.Icon = SystemIcons.Application;
            }
        }
        
        /// <summary>
        /// Configure le menu principal
        /// </summary>
        public MenuStrip CreateMainMenu(
            EventHandler settingsClickHandler, 
            EventHandler configAppsClickHandler)
        {
            // Menu principal
            MenuStrip mainMenu = new MenuStrip
            {
                BackColor = Color.White,
                Renderer = new ToolStripProfessionalRenderer(new UIHelpers.CustomMenuColorTable())
            };
            
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("Fichier");
            ToolStripMenuItem settingsMenu = new ToolStripMenuItem("Paramètres");
            ToolStripMenuItem configAppsMenu = new ToolStripMenuItem("Configurer les applications");
            ToolStripMenuItem exitMenu = new ToolStripMenuItem("Quitter");
            
            fileMenu.DropDownItems.Add(settingsMenu);
            fileMenu.DropDownItems.Add(configAppsMenu);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitMenu);
            
            mainMenu.Items.Add(fileMenu);
            
            // Événements
            if (settingsClickHandler != null)
                settingsMenu.Click += settingsClickHandler;
                
            if (configAppsClickHandler != null)
                configAppsMenu.Click += configAppsClickHandler;
                
            exitMenu.Click += (s, e) => _form.Close();
            
            return mainMenu;
        }
        
        /// <summary>
        /// Configure l'événement de chargement du formulaire
        /// </summary>
        public void ConfigureLoadEvent(
            Action updateApplicationsListAction, 
            Action ensureSafeSplitterDistanceAction,
            Func<bool> shouldShowDialogFunc)
        {
            _form.Load += (s, e) => {
                // Mettre à jour la liste des applications surveillées avec les applications par défaut
                updateApplicationsListAction?.Invoke();
                
                // S'assurer que le titre est bien défini
                _form.Text = "AutosavePro";
                
                // Ajouter un dernier filet de sécurité après le chargement complet
                _form.BeginInvoke(new Action(() => ensureSafeSplitterDistanceAction?.Invoke()));
                
                // Afficher le dialogue de bienvenue après un court délai si nécessaire
                if (shouldShowDialogFunc != null && shouldShowDialogFunc())
                {
                    ShowWelcomeDialog();
                }
            };
        }
        
        /// <summary>
        /// Affiche le dialogue de bienvenue
        /// </summary>
        private void ShowWelcomeDialog()
        {
            System.Windows.Forms.Timer welcomeTimer = new System.Windows.Forms.Timer();
            welcomeTimer.Interval = 500; // 500ms après le chargement
            welcomeTimer.Tick += (s, ev) => {
                welcomeTimer.Stop();
                welcomeTimer.Dispose();
                
                using (WelcomeDialog welcomeDialog = new WelcomeDialog())
                {
                    welcomeDialog.ShowDialog(_form);
                }
            };
            welcomeTimer.Start();
        }
    }
} 