using System.ComponentModel;

namespace WpfApp1.ViewModels
{
    /// <summary>
    /// Classe che fungerà da base per venire ereditata dalle altre.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Ogni cambio in una proprietà genererà 
        /// un evento di notifica.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
