using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// Basis-ViewModel mit INotifyPropertyChanged-Implementation
    /// Erweitert für v1.9.0 mit SetProperty-Methode
    /// </summary>
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Setzt eine Property und benachrichtigt über Änderungen
        /// </summary>
        /// <typeparam name="T">Property-Type</typeparam>
        /// <param name="field">Backing field</param>
        /// <param name="value">Neuer Wert</param>
        /// <param name="propertyName">Property-Name (automatisch)</param>
        /// <returns>True wenn sich der Wert geändert hat</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
