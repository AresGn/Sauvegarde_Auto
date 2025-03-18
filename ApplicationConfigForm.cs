using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;

namespace AutosavePro
{
    public class ApplicationConfigForm : Form
    {
        private ApplicationConfig _config;
        private bool _isNewConfig;
        private TextBox _appNameTextBox;
        private TextBox _processNameTextBox;
        private NumericUpDown _initialDelayNumeric;
        private NumericUpDown _finalDelayNumeric;
        private TextBox _indicatorsTextBox;
        private TextBox _exclusionsTextBox;
        private TextBox _titlePrefixTextBox;
        private TextBox _titleSuffixTextBox;
        private TextBox _patternTextBox;
        private ListView _pathsListView;
        private Button _addPathButton;
        private Button _removePathButton;
        private Button _saveButton;
        private Button _cancelButton;
        
        public ApplicationConfigForm(ApplicationConfig config, bool isNew = false)
        {
            _config = config;
            _isNewConfig = isNew;
            
            InitializeComponents();
            LoadConfigData();
        }
        
        private void InitializeComponents()
        {
            this.Text = _isNewConfig 
                ? "Nouvelle configuration d'application" 
                : $"Configuration de {_config.AppName}";
            this.Size = new Size(700, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // Ajout d'un ToolTip pour les contrôles
            ToolTip toolTip = new ToolTip();
            
            // Panel principal
            TableLayoutPanel mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20)
            };
            
            // Zone des propriétés de base
            GroupBox basicPropsGroup = new GroupBox
            {
                Text = "Propriétés de base",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            TableLayoutPanel basicPropsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 30),
                    new ColumnStyle(SizeType.Percent, 70)
                }
            };
            
            // Ajout des contrôles pour les propriétés de base
            basicPropsPanel.Controls.Add(new Label { Text = "Nom de l'application:", Anchor = AnchorStyles.Left }, 0, 0);
            _appNameTextBox = new TextBox { Width = 250, Anchor = AnchorStyles.Left };
            basicPropsPanel.Controls.Add(_appNameTextBox, 1, 0);
            
            basicPropsPanel.Controls.Add(new Label { Text = "Nom du processus:", Anchor = AnchorStyles.Left }, 0, 1);
            _processNameTextBox = new TextBox { Width = 250, Anchor = AnchorStyles.Left };
            basicPropsPanel.Controls.Add(_processNameTextBox, 1, 1);
            
            basicPropsPanel.Controls.Add(new Label { Text = "Délai initial (ms):", Anchor = AnchorStyles.Left }, 0, 2);
            _initialDelayNumeric = new NumericUpDown { 
                Minimum = 10, 
                Maximum = 2000, 
                Increment = 10,
                Width = 100,
                Anchor = AnchorStyles.Left
            };
            basicPropsPanel.Controls.Add(_initialDelayNumeric, 1, 2);
            
            basicPropsPanel.Controls.Add(new Label { Text = "Délai final (ms):", Anchor = AnchorStyles.Left }, 0, 3);
            _finalDelayNumeric = new NumericUpDown { 
                Minimum = 10, 
                Maximum = 2000, 
                Increment = 10,
                Width = 100,
                Anchor = AnchorStyles.Left
            };
            basicPropsPanel.Controls.Add(_finalDelayNumeric, 1, 3);
            
            basicPropsGroup.Controls.Add(basicPropsPanel);
            
            // Zone des indicateurs et patterns
            GroupBox patternsGroup = new GroupBox
            {
                Text = "Indicateurs et motifs",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            TableLayoutPanel patternsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 30),
                    new ColumnStyle(SizeType.Percent, 70)
                }
            };
            
            // Ajout des contrôles pour les patterns
            patternsPanel.Controls.Add(new Label { 
                Text = "Indicateurs de modification:",
                Anchor = AnchorStyles.Left,
                AutoSize = true
            }, 0, 0);
            _indicatorsTextBox = new TextBox { 
                Width = 300, 
                Anchor = AnchorStyles.Left
            };
            toolTip.SetToolTip(_indicatorsTextBox, "Séparés par des virgules (ex: *, •, (modifié))");
            patternsPanel.Controls.Add(_indicatorsTextBox, 1, 0);
            
            patternsPanel.Controls.Add(new Label { 
                Text = "Motifs à exclure:",
                Anchor = AnchorStyles.Left,
                AutoSize = true
            }, 0, 1);
            _exclusionsTextBox = new TextBox { 
                Width = 300, 
                Anchor = AnchorStyles.Left
            };
            toolTip.SetToolTip(_exclusionsTextBox, "Séparés par des virgules (ex: [Lecture seule], [Mode protégé])");
            patternsPanel.Controls.Add(_exclusionsTextBox, 1, 1);
            
            patternsPanel.Controls.Add(new Label { 
                Text = "Préfixe du titre:",
                Anchor = AnchorStyles.Left
            }, 0, 2);
            _titlePrefixTextBox = new TextBox { Width = 300, Anchor = AnchorStyles.Left };
            patternsPanel.Controls.Add(_titlePrefixTextBox, 1, 2);
            
            patternsPanel.Controls.Add(new Label { 
                Text = "Suffixe du titre:",
                Anchor = AnchorStyles.Left
            }, 0, 3);
            _titleSuffixTextBox = new TextBox { Width = 300, Anchor = AnchorStyles.Left };
            patternsPanel.Controls.Add(_titleSuffixTextBox, 1, 3);
            
            patternsPanel.Controls.Add(new Label { 
                Text = "Expression régulière:",
                Anchor = AnchorStyles.Left
            }, 0, 4);
            _patternTextBox = new TextBox { Width = 300, Anchor = AnchorStyles.Left };
            patternsPanel.Controls.Add(_patternTextBox, 1, 4);
            
            patternsGroup.Controls.Add(patternsPanel);
            
            // Zone des chemins supplémentaires
            GroupBox pathsGroup = new GroupBox
            {
                Text = "Chemins de recherche supplémentaires",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            TableLayoutPanel pathsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 80),
                    new ColumnStyle(SizeType.Percent, 20)
                }
            };
            
            // Liste des chemins
            _pathsListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true
            };
            _pathsListView.Columns.Add("Chemin", -2);
            
            pathsPanel.Controls.Add(_pathsListView, 0, 0);
            
            // Boutons pour gérer les chemins
            Panel pathButtonsPanel = new Panel { Dock = DockStyle.Fill };
            
            _addPathButton = new Button
            {
                Text = "Ajouter...",
                Width = 100,
                Location = new Point(10, 10)
            };
            _addPathButton.Click += AddPath_Click;
            
            _removePathButton = new Button
            {
                Text = "Supprimer",
                Width = 100,
                Location = new Point(10, 40)
            };
            _removePathButton.Click += RemovePath_Click;
            
            pathButtonsPanel.Controls.Add(_addPathButton);
            pathButtonsPanel.Controls.Add(_removePathButton);
            
            pathsPanel.Controls.Add(pathButtonsPanel, 1, 0);
            
            pathsGroup.Controls.Add(pathsPanel);
            
            // Boutons de validation
            Panel buttonsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };
            
            _saveButton = new Button
            {
                Text = "Enregistrer",
                Width = 100,
                Location = new Point(this.Width - 230, 10),
                Anchor = AnchorStyles.Right
            };
            _saveButton.Click += Save_Click;
            
            _cancelButton = new Button
            {
                Text = "Annuler",
                Width = 100,
                Location = new Point(this.Width - 120, 10),
                Anchor = AnchorStyles.Right
            };
            _cancelButton.Click += Cancel_Click;
            
            buttonsPanel.Controls.Add(_saveButton);
            buttonsPanel.Controls.Add(_cancelButton);
            
            // Assemblage final
            mainPanel.Controls.Add(basicPropsGroup);
            mainPanel.Controls.Add(patternsGroup);
            mainPanel.Controls.Add(pathsGroup);
            
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            
            this.Controls.Add(mainPanel);
            this.Controls.Add(buttonsPanel);
        }
        
        private void LoadConfigData()
        {
            // Propriétés de base
            _appNameTextBox.Text = _config.AppName;
            _processNameTextBox.Text = _config.ProcessName;
            _initialDelayNumeric.Value = _config.InitialDelay;
            _finalDelayNumeric.Value = _config.FinalDelay;
            
            // Indicateurs et patterns
            _indicatorsTextBox.Text = string.Join(", ", _config.ModificationIndicators);
            _exclusionsTextBox.Text = string.Join(", ", _config.ExclusionPatterns);
            _titlePrefixTextBox.Text = _config.TitlePrefix ?? "";
            _titleSuffixTextBox.Text = _config.TitleSuffix ?? "";
            _patternTextBox.Text = _config.FileTitlePattern ?? "";
            
            // Chemins supplémentaires
            foreach (var path in _config.AdditionalSearchPaths)
            {
                _pathsListView.Items.Add(path);
            }
        }
        
        private void AddPath_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Sélectionnez un dossier à surveiller";
                dialog.ShowNewFolderButton = true;
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string path = dialog.SelectedPath;
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        _pathsListView.Items.Add(path);
                    }
                }
            }
        }
        
        private void RemovePath_Click(object sender, EventArgs e)
        {
            if (_pathsListView.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in _pathsListView.SelectedItems)
                {
                    _pathsListView.Items.Remove(item);
                }
            }
        }
        
        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                // Validation de base
                if (string.IsNullOrEmpty(_appNameTextBox.Text) || string.IsNullOrEmpty(_processNameTextBox.Text))
                {
                    MessageBox.Show("Le nom de l'application et le nom du processus sont obligatoires.", 
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Mise à jour de la configuration
                _config.AppName = _appNameTextBox.Text;
                _config.ProcessName = _processNameTextBox.Text;
                _config.InitialDelay = (int)_initialDelayNumeric.Value;
                _config.FinalDelay = (int)_finalDelayNumeric.Value;
                
                // Mise à jour des indicateurs et patterns
                _config.ModificationIndicators = _indicatorsTextBox.Text
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();
                    
                _config.ExclusionPatterns = _exclusionsTextBox.Text
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();
                    
                _config.TitlePrefix = string.IsNullOrWhiteSpace(_titlePrefixTextBox.Text) ? null : _titlePrefixTextBox.Text;
                _config.TitleSuffix = string.IsNullOrWhiteSpace(_titleSuffixTextBox.Text) ? null : _titleSuffixTextBox.Text;
                _config.FileTitlePattern = string.IsNullOrWhiteSpace(_patternTextBox.Text) ? null : _patternTextBox.Text;
                
                // Mise à jour des chemins
                _config.AdditionalSearchPaths.Clear();
                foreach (ListViewItem item in _pathsListView.Items)
                {
                    _config.AdditionalSearchPaths.Add(item.Text);
                }
                
                // Enregistrer la configuration
                ApplicationRegistry.Instance.AddApplicationConfig(_config);
                
                Logger.Log($"Configuration enregistrée pour {_config.AppName}", LogLevel.Info);
                
                DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}", 
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void Cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
} 