using System;

namespace AutosavePro.Models
{
    /// <summary>
    /// Représente un intervalle de temps pour la sauvegarde automatique
    /// </summary>
    public class SaveInterval
    {
        public string Text { get; set; }
        public int Value { get; set; }
        
        public SaveInterval(string text, int value)
        {
            Text = text;
            Value = value;
        }
        
        public override string ToString()
        {
            return Text;
        }
    }
} 