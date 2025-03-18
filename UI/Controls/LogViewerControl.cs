using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutosavePro.UI.Controls
{
    /// <summary>
    /// Contrôle permettant d'afficher les logs de l'application
    /// </summary>
    public class LogViewerControl : UserControl
    {
        private TextBox _logsTextBox;
        private Panel _headerPanel;
        private Label _headerLabel;
        
        public LogViewerControl()
        {
            InitializeComponent();
            
            // S'abonner aux événements du logger
            Logger.OnNewLog += OnNewLogReceived;
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Création du panel d'en-tête
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(245, 245, 247)
            };
            
            _headerLabel = new Label
            {
                Text = "Historique des opérations",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 0, 0)
            };
            _headerPanel.Controls.Add(_headerLabel);
            
            // Création de la zone de texte pour les logs
            _logsTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10f),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            
            // Ajout des contrôles
            this.Controls.Add(_logsTextBox);
            this.Controls.Add(_headerPanel);
            
            // Configuration du UserControl
            this.Dock = DockStyle.Fill;
            this.ResumeLayout(false);
            
            // Charger les logs existants
            LoadInitialLogs();
        }
        
        private void OnNewLogReceived(object? sender, LogEntry logEntry)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<LogEntry>(AppendLogEntry), logEntry);
            }
            else
            {
                AppendLogEntry(logEntry);
            }
        }
        
        private void AppendLogEntry(LogEntry logEntry)
        {
            _logsTextBox.AppendText(logEntry.ToString() + Environment.NewLine);
            _logsTextBox.SelectionStart = _logsTextBox.TextLength;
            _logsTextBox.ScrollToCaret();
        }
        
        private void LoadInitialLogs()
        {
            var recentLogs = Logger.GetRecentLogs();
            foreach (var log in recentLogs)
            {
                AppendLogEntry(log);
            }
        }
        
        /// <summary>
        /// Rafraîchit l'affichage des logs
        /// </summary>
        public void RefreshLogs()
        {
            // Le contrôle s'auto-rafraîchit via les événements de Logger,
            // donc cette méthode est principalement une interface pour permettre
            // l'appel depuis MainForm
            
            // Forcer le défilement vers le bas si nécessaire
            _logsTextBox.SelectionStart = _logsTextBox.TextLength;
            _logsTextBox.ScrollToCaret();
        }
        
        /// <summary>
        /// Vide l'affichage des logs
        /// </summary>
        public void ClearLogs()
        {
            if (_logsTextBox.InvokeRequired)
            {
                _logsTextBox.Invoke(new Action(ClearLogs));
                return;
            }

            _logsTextBox.Clear();
        }
        
        /// <summary>
        /// Libération des ressources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Se désabonner des événements
                Logger.OnNewLog -= OnNewLogReceived;
            }
            
            base.Dispose(disposing);
        }
    }
} 