using System;
using System.Timers;

namespace AutosavePro.Helpers
{
    /// <summary>
    /// Gère le minuteur de sauvegarde automatique
    /// </summary>
    public class AutoSaveTimer : IDisposable
    {
        private readonly string _appName;
        private System.Timers.Timer _timer;
        private TimeSpan _interval;
        private bool _disposed;

        /// <summary>
        /// Événement déclenché lorsque le minuteur expire
        /// </summary>
        public event ElapsedEventHandler TimerElapsed;

        public AutoSaveTimer(string appName, TimeSpan interval)
        {
            _appName = appName;
            _interval = interval;
            _disposed = false;
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _timer = new System.Timers.Timer
            {
                Interval = _interval.TotalMilliseconds,
                AutoReset = true,
                Enabled = true
            };
            _timer.Elapsed += OnTimerElapsed;
            Logger.Log($"Minuteur d'auto-sauvegarde initialisé pour {_appName} ({_interval.TotalMinutes} minutes)");
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            TimerElapsed?.Invoke(this, e);
        }

        /// <summary>
        /// Définit un nouvel intervalle pour le minuteur
        /// </summary>
        public void SetInterval(TimeSpan interval)
        {
            _interval = interval;
            _timer.Interval = interval.TotalMilliseconds;
            Logger.Log($"Intervalle de sauvegarde mis à jour pour {_appName} : {interval.TotalMinutes} minutes");
        }

        /// <summary>
        /// Active ou désactive le minuteur
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _timer.Enabled = enabled;
            string status = enabled ? "activé" : "désactivé";
            Logger.Log($"Minuteur d'auto-sauvegarde {status} pour {_appName}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _timer?.Dispose();
                }
                _disposed = true;
            }
        }
    }
} 