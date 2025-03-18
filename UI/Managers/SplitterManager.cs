using System;
using System.Windows.Forms;

namespace AutosavePro.UI.Managers
{
    /// <summary>
    /// Gestionnaire du SplitContainer pour maintenir une division correcte
    /// </summary>
    public class SplitterManager
    {
        private readonly SplitContainer _splitContainer;
        private readonly Form _ownerForm;
        private System.Windows.Forms.Timer? _sizeChangeTimer = null;
        
        public SplitterManager(SplitContainer splitContainer, Form ownerForm)
        {
            _splitContainer = splitContainer ?? throw new ArgumentNullException(nameof(splitContainer));
            _ownerForm = ownerForm ?? throw new ArgumentNullException(nameof(ownerForm));
            
            Initialize();
        }
        
        /// <summary>
        /// Initialise le gestionnaire et définit les événements
        /// </summary>
        private void Initialize()
        {
            // Configurer le comportement du splitter
            _splitContainer.Panel1MinSize = 100;
            _splitContainer.Panel2MinSize = 100;
            _splitContainer.SplitterWidth = 5;
            
            // Configuration visuelle des panneaux
            _splitContainer.Panel1.BackColor = System.Drawing.Color.White;
            _splitContainer.Panel2.BackColor = System.Drawing.Color.White;
            
            // Configurer SplitterDistance seulement APRÈS avoir ajouté le contrôle à la hiérarchie
            _splitContainer.HandleCreated += (s, e) => {
                try {
                    // Attendre que le contrôle soit complètement créé et mesuré
                    _ownerForm.BeginInvoke(new Action(() => {
                        try {
                            if (_splitContainer.Width > 0) {
                                SetInitialSplitterDistance();
                            }
                        } catch (Exception ex) {
                            Logger.Log($"Erreur lors de l'initialisation du splitter: {ex.Message}", LogLevel.Error);
                        }
                    }));
                } catch (Exception ex) {
                    Logger.Log($"Erreur lors de la configuration du splitter: {ex.Message}", LogLevel.Error);
                }
            };
            
            // Écouter les changements de taille du formulaire parent
            _ownerForm.SizeChanged += HandleSizeChanged;
            
            // Écouter les déplacements du splitter
            _splitContainer.SplitterMoved += (s, e) => {
                // Vérifier après un court délai pour éviter les boucles infinies
                var checkTimer = new System.Windows.Forms.Timer {
                    Interval = 50
                };
                checkTimer.Tick += (s2, e2) => {
                    checkTimer.Stop();
                    checkTimer.Dispose();
                    EnsureSafeSplitterDistance();
                };
                checkTimer.Start();
            };
        }
        
        /// <summary>
        /// Gère le changement de taille du formulaire
        /// </summary>
        private void HandleSizeChanged(object? sender, EventArgs e)
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
        
        /// <summary>
        /// Définit la position initiale du splitter
        /// </summary>
        private void SetInitialSplitterDistance()
        {
            int newSplitterDistance = _splitContainer.Width / 2;
            
            // Vérifier que la distance est valide
            if (newSplitterDistance < _splitContainer.Panel1MinSize) {
                newSplitterDistance = _splitContainer.Panel1MinSize;
            }
            
            int maxDistance = _splitContainer.Width - _splitContainer.Panel2MinSize - _splitContainer.SplitterWidth;
            if (newSplitterDistance > maxDistance) {
                newSplitterDistance = maxDistance;
            }
            
            _splitContainer.SplitterDistance = newSplitterDistance;
        }
        
        /// <summary>
        /// S'assure que la distance du splitter est toujours valide
        /// </summary>
        public void EnsureSafeSplitterDistance()
        {
            try
            {
                if (_splitContainer == null || !_splitContainer.IsHandleCreated || _splitContainer.Width <= 0)
                    return;
                
                // Vérifier d'abord si l'espace est suffisant
                int totalWidth = _splitContainer.Width;
                int minRequired = _splitContainer.Panel1MinSize + _splitContainer.Panel2MinSize + _splitContainer.SplitterWidth;
                
                // Si la fenêtre est trop petite, réduire les tailles minimales
                if (totalWidth < minRequired)
                {
                    int newMinSize = Math.Max(50, (totalWidth - _splitContainer.SplitterWidth) / 3);
                    _splitContainer.Panel1MinSize = newMinSize;
                    _splitContainer.Panel2MinSize = newMinSize;
                    Logger.Log($"Tailles minimales des panneaux réduites à {newMinSize}px pour s'adapter à l'écran", LogLevel.Warning);
                }
                
                // Maintenant, calculer les limites valides
                int maxSplitterPosition = _splitContainer.Width - _splitContainer.Panel2MinSize - _splitContainer.SplitterWidth;
                int minSplitterPosition = _splitContainer.Panel1MinSize;
                
                // Vérifier si la position actuelle est valide et l'ajuster si nécessaire
                if (_splitContainer.SplitterDistance < minSplitterPosition)
                {
                    _splitContainer.SplitterDistance = minSplitterPosition;
                    Logger.Log($"SplitterDistance corrigé au minimum: {minSplitterPosition}", LogLevel.Debug);
                }
                else if (_splitContainer.SplitterDistance > maxSplitterPosition)
                {
                    _splitContainer.SplitterDistance = maxSplitterPosition;
                    Logger.Log($"SplitterDistance corrigé au maximum: {maxSplitterPosition}", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la vérification du SplitterDistance: {ex.Message}", LogLevel.Error);
            }
        }
        
        /// <summary>
        /// Libère les ressources
        /// </summary>
        public void Dispose()
        {
            _sizeChangeTimer?.Dispose();
            _ownerForm.SizeChanged -= HandleSizeChanged;
        }
    }
} 