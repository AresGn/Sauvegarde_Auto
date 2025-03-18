using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutosavePro.Helpers
{
    /// <summary>
    /// Classe d'utilitaires pour l'interface utilisateur
    /// </summary>
    public static class UIHelpers
    {
        /// <summary>
        /// Crée un bouton moderne avec un style plat et une couleur personnalisée
        /// </summary>
        public static Button CreateModernButton(string text, Color color)
        {
            Button button = new Button
            {
                Text = text,
                Width = 120,
                Height = 32,
                Margin = new Padding(10, 5, 10, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                FlatAppearance = {
                    BorderSize = 0,
                    MouseOverBackColor = ControlPaint.Light(color),
                    MouseDownBackColor = ControlPaint.Dark(color)
                }
            };
            return button;
        }
        
        /// <summary>
        /// Classe personnalisée pour les couleurs du menu
        /// </summary>
        public class CustomMenuColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => Color.FromArgb(245, 245, 247);
            public override Color MenuItemBorder => Color.FromArgb(240, 240, 242);
            public override Color MenuBorder => Color.FromArgb(230, 230, 235);
            public override Color ToolStripDropDownBackground => Color.White;
        }
        
        /// <summary>
        /// Crée un nouveau TabControl avec un style moderne
        /// </summary>
        public static TabControl CreateModernTabControl()
        {
            return new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f),
                Appearance = TabAppearance.FlatButtons,
                ItemSize = new Size(100, 30),
                SizeMode = TabSizeMode.Fixed,
                Padding = new Point(10, 3)
            };
        }
    }
} 