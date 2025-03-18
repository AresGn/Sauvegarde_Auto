using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutosavePro
{
    public class WelcomeDialog : Form
    {
        private CheckBox? _dontShowAgainCheckBox;
        private const string RegistryKeyName = "AutosaveProSettings";
        private const string DontShowWelcomeKey = "DontShowWelcome";
        
        public WelcomeDialog()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            // Configuration de base de la fenêtre
            this.Text = "Bienvenue dans AutosavePro";
            this.Size = new Size(600, 550);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            
            // Essayer de charger l'icône
            try
            {
                if (System.IO.File.Exists("autosave.ico"))
                {
                    this.Icon = new Icon("autosave.ico");
                }
            }
            catch
            {
                // Ignorer les erreurs d'icône
            }
            
            // Panel principal avec plus d'espace
            TableLayoutPanel mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(30),
                BackColor = Color.White
            };
            
            // Zone du logo
            Panel logoPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 80
            };
            
            Label logoLabel = new Label
            {
                Text = "AutosavePro",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            
            logoPanel.Controls.Add(logoLabel);
            
            // Zone de message principal
            Panel messagePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };
            
            TableLayoutPanel textLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                RowStyles = {
                    new RowStyle(SizeType.Percent, 70),
                    new RowStyle(SizeType.Percent, 30)
                }
            };

            // Label pour les instructions principales
            Label instructionsLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.FromArgb(60, 60, 60),
                TextAlign = ContentAlignment.TopLeft
            };

            // Label pour le message de conclusion
            Label conclusionLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.TopLeft
            };

            // Texte des instructions
            instructionsLabel.Text = "Finies les pertes de données !\n\n" +
                                   "Avec AutosavePro, protégez votre travail automatiquement :\n\n" +
                                   "1. Sélectionnez un intervalle de sauvegarde\n\n" +
                                   "2. Activez le bouton \"Sauvegarde automatique\"\n\n" +
                                   "3. Fermez l'application et laissez la magie opérer !";

            // Texte de conclusion
            conclusionLabel.Text = "\nVos documents seront sauvegardés en arrière-plan\nsans interrompre votre travail.";

            textLayoutPanel.Controls.Add(instructionsLabel, 0, 0);
            textLayoutPanel.Controls.Add(conclusionLabel, 0, 1);
            messagePanel.Controls.Add(textLayoutPanel);
            
            // Zone de boutons
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(0, 10, 0, 0)
            };
            
            Button okButton = new Button
            {
                Text = "Commencer",
                Width = 140,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right
            };
            
            okButton.FlatAppearance.BorderSize = 0;
            okButton.Location = new Point(buttonPanel.Width - okButton.Width - 20, 10);
            okButton.Click += (s, e) => this.Close();
            
            _dontShowAgainCheckBox = new CheckBox
            {
                Text = "Ne plus afficher ce message",
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(0, 20),
                Anchor = AnchorStyles.Left
            };
            
            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(_dontShowAgainCheckBox);
            
            // Assemblage de la mise en page avec plus d'espace pour le contenu
            mainPanel.Controls.Add(logoPanel);
            mainPanel.Controls.Add(messagePanel);
            mainPanel.Controls.Add(buttonPanel);
            
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            
            this.Controls.Add(mainPanel);
            
            // Gestion de la fermeture pour sauvegarder le choix
            this.FormClosing += (s, e) => {
                if (_dontShowAgainCheckBox?.Checked == true)
                {
                    try
                    {
                        Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegistryKeyName)
                            ?.SetValue(DontShowWelcomeKey, true);
                    }
                    catch
                    {
                        // Ignorer les erreurs de registre
                    }
                }
            };
        }
        
        // Méthode statique pour vérifier s'il faut afficher le dialogue
        public static bool ShouldShowDialog()
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryKeyName);
                if (key != null)
                {
                    var value = key.GetValue(DontShowWelcomeKey);
                    return value == null || !(bool)value;
                }
            }
            catch
            {
                // En cas d'erreur, on affiche le dialogue par défaut
            }
            
            return true;
        }
    }
} 