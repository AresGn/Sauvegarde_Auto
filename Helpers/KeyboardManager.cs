using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutosavePro.Helpers
{
    /// <summary>
    /// Gère l'envoi de commandes clavier aux applications
    /// </summary>
    public class KeyboardManager
    {
        // Définition des constantes pour l'envoi de touches
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_S = 0x53;
        
        // Autres constantes Win32
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // Structures et constantes pour SendInput
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION union;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mouseInput;
            [FieldOffset(0)]
            public KEYBDINPUT keyboardInput;
            [FieldOffset(0)]
            public HARDWAREINPUT hardwareInput;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        private readonly string _appName;
        private readonly int _initialDelay;
        private readonly int _finalDelay;
        private readonly bool _useAdvancedMethod;

        public KeyboardManager(string appName, int initialDelay = 100, int finalDelay = 100, bool useAdvancedMethod = false)
        {
            _appName = appName;
            _initialDelay = initialDelay;
            _finalDelay = finalDelay;
            _useAdvancedMethod = useAdvancedMethod;
        }

        /// <summary>
        /// Envoie la combinaison Ctrl+S à une fenêtre spécifique
        /// </summary>
        public bool SendCtrlS(IntPtr windowHandle)
        {
            if (_useAdvancedMethod)
            {
                return SendCtrlSWithSendInput(windowHandle);
            }
            else
            {
                return SendCtrlSWithKeyboardEvent(windowHandle);
            }
        }

        /// <summary>
        /// Envoie Ctrl+S à une fenêtre spécifique avec la méthode standard
        /// </summary>
        private bool SendCtrlSWithKeyboardEvent(IntPtr windowHandle)
        {
            try
            {
                if (windowHandle == IntPtr.Zero)
                {
                    Logger.Log("Impossible d'envoyer Ctrl+S - Handle de fenêtre invalide", LogLevel.Warning);
                    return false;
                }
                
                // Mémoriser la fenêtre qui avait le focus avant
                IntPtr currentForegroundWindow = GetForegroundWindow();
                
                // Donner le focus à la fenêtre cible
                if (!SetForegroundWindow(windowHandle))
                {
                    Logger.Log("Impossible de donner le focus à la fenêtre pour envoyer Ctrl+S", LogLevel.Warning);
                    return false;
                }
                
                // Petit délai pour s'assurer que la fenêtre a bien le focus
                Thread.Sleep(_initialDelay);
                
                // Simuler Ctrl+S en utilisant keybd_event
                // Appuyer sur Ctrl
                keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
                // Appuyer sur S
                keybd_event(VK_S, 0, 0, UIntPtr.Zero);
                // Relâcher S
                keybd_event(VK_S, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                // Relâcher Ctrl
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                
                // Petit délai pour laisser le temps à l'application de traiter la commande
                Thread.Sleep(_finalDelay);
                
                // Restaurer le focus à la fenêtre précédente
                if (currentForegroundWindow != IntPtr.Zero && currentForegroundWindow != windowHandle)
                {
                    SetForegroundWindow(currentForegroundWindow);
                }
                
                Logger.Log($"Commande Ctrl+S envoyée à la fenêtre de {_appName}", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de l'envoi de Ctrl+S: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Envoie Ctrl+S à une fenêtre spécifique avec la méthode avancée SendInput (plus fiable)
        /// </summary>
        private bool SendCtrlSWithSendInput(IntPtr windowHandle)
        {
            try
            {
                if (windowHandle == IntPtr.Zero)
                {
                    Logger.Log("Impossible d'envoyer Ctrl+S - Handle de fenêtre invalide", LogLevel.Warning);
                    return false;
                }
                
                // Mémoriser la fenêtre qui avait le focus avant
                IntPtr currentForegroundWindow = GetForegroundWindow();
                
                // Donner le focus à la fenêtre cible
                if (!SetForegroundWindow(windowHandle))
                {
                    Logger.Log("Impossible de donner le focus à la fenêtre pour envoyer Ctrl+S", LogLevel.Warning);
                    return false;
                }
                
                // Délai plus long pour s'assurer que la fenêtre a bien le focus
                Thread.Sleep(_initialDelay);
                
                // Utiliser SendInput pour envoyer Ctrl+S
                INPUT[] inputs = new INPUT[4];
                
                // Appuyer sur Ctrl
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].union.keyboardInput.wVk = VK_CONTROL;
                inputs[0].union.keyboardInput.dwFlags = 0;
                
                // Appuyer sur S
                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].union.keyboardInput.wVk = VK_S;
                inputs[1].union.keyboardInput.dwFlags = 0;
                
                // Relâcher S
                inputs[2].type = INPUT_KEYBOARD;
                inputs[2].union.keyboardInput.wVk = VK_S;
                inputs[2].union.keyboardInput.dwFlags = KEYEVENTF_KEYUP;
                
                // Relâcher Ctrl
                inputs[3].type = INPUT_KEYBOARD;
                inputs[3].union.keyboardInput.wVk = VK_CONTROL;
                inputs[3].union.keyboardInput.dwFlags = KEYEVENTF_KEYUP;
                
                // Envoyer les inputs
                uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                
                if (result != inputs.Length)
                {
                    Logger.Log($"Erreur lors de l'envoi de Ctrl+S avec SendInput: {Marshal.GetLastWin32Error()}", LogLevel.Warning);
                    return false;
                }
                
                // Délai plus long pour laisser le temps à l'application de traiter la commande
                Thread.Sleep(_finalDelay);
                
                // Restaurer le focus à la fenêtre précédente
                if (currentForegroundWindow != IntPtr.Zero && currentForegroundWindow != windowHandle)
                {
                    SetForegroundWindow(currentForegroundWindow);
                }
                
                Logger.Log($"Commande Ctrl+S envoyée à la fenêtre de {_appName} avec SendInput", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de l'envoi de Ctrl+S avec SendInput: {ex.Message}", LogLevel.Error);
                return false;
            }
        }
    }
} 