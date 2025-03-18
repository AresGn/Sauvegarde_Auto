using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using AutosavePro.UI.Managers;
using AutosavePro.Helpers;

namespace AutosavePro.UI.Controls
{
    /// <summary>
    /// Contrôle permettant d'afficher et gérer la liste des applications
    /// </summary>
    public class ApplicationListControl : UserControl
    {
        private ListView _listView;
        private Panel _headerPanel;
        private Label _headerLabel;
        private IconManager _iconManager;
        
        public event EventHandler<InstalledApplication>? ApplicationSelected;
        public event EventHandler? RefreshRequested;
        
        /// <summary>
        /// Récupère la listView pour permettre l'accès externe
        /// </summary>
        public ListView ListView => _listView;
        
        /// <summary>
        /// Récupère ou définit le titre du contrôle
        /// </summary>
        [System.ComponentModel.Browsable(true)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Visible)]
        [System.ComponentModel.Description("Le titre affiché en en-tête du contrôle")]
        public string Title
        {
            get => _headerLabel.Text;
            set => _headerLabel.Text = value;
        }
        
        public ApplicationListControl(IconManager iconManager)
        {
            _iconManager = iconManager ?? throw new ArgumentNullException(nameof(iconManager));
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            // Configuration de base du contrôle
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            
            // Création du panel pour le titre
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(245, 245, 247)
            };
            
            _headerLabel = new Label
            {
                Text = "Applications",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 0, 0)
            };
            
            _headerPanel.Controls.Add(_headerLabel);
            
            // Création de la liste des applications
            _listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                SmallImageList = _iconManager.AppImageList,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.None
            };
            
            // Gestion des événements
            _listView.ItemSelectionChanged += (s, e) => {
                if (_listView.SelectedItems.Count > 0 && 
                    _listView.SelectedItems[0].Tag is InstalledApplication app)
                {
                    ApplicationSelected?.Invoke(this, app);
                }
            };
            
            // Ajout des contrôles au contrôle parent
            this.Controls.Add(_listView);
            this.Controls.Add(_headerPanel);
        }
        
        /// <summary>
        /// Configure les colonnes de la ListView
        /// </summary>
        public void ConfigureColumns(ColumnHeader[] columns)
        {
            _listView.Columns.Clear();
            _listView.Columns.AddRange(columns);
        }
        
        /// <summary>
        /// Ajoute un élément à la ListView
        /// </summary>
        public void AddItem(ListViewItem item)
        {
            _listView.Items.Add(item);
        }
        
        /// <summary>
        /// Supprime tous les éléments de la ListView
        /// </summary>
        public void ClearItems()
        {
            _listView.Items.Clear();
        }
        
        /// <summary>
        /// Rafraîchit la liste des applications
        /// </summary>
        public void RefreshList()
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }
} 