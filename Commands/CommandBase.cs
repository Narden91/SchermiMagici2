using System;
using System.Windows.Input;

namespace WpfApp1.Commands
{
    public abstract class CommandBase : ICommand
    {
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Metodo per capire se il Comando può o non può essere eseguito
        /// Se ritorna False il bottone è disabilitato
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual bool CanExecute(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Ciò che viene eseguito quando viene premuto il bottone associato
        /// </summary>
        /// <param name="parameter"></param>
        /// <exception cref="NotImplementedException"></exception>
        public abstract void Execute(object parameter);

        /// <summary>
        /// Helper function per avere un metodo diretto per
        /// effettuare il trigger dell'evento CanExecuteChanged
        /// </summary>
        /// <param name="parameter"></param>
        protected void OnCanExecuteChanged(object parameter)
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }
    }
}
