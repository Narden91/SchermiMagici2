using System.IO;
using System.Windows;
using WpfApp1.Stores;
using WpfApp1.ViewModels;


namespace WpfApp1.Commands
{
    public class AddPathTaskCommand : CommandBase
    {
        private readonly ProctorPersonalInfoPageViewModel _proctorInfoViewModel;
        private readonly ProctorStore _proctorStore;

        public AddPathTaskCommand(ProctorPersonalInfoPageViewModel proctorInfoViewModel, ProctorStore proctorStore)
        {
            _proctorInfoViewModel = proctorInfoViewModel;
            _proctorStore = proctorStore;
        }

        /// <summary>
        /// Apre BrowserDialog per selezionare la cartella nella quale sono contenute le immagini da usare come task
        /// </summary>
        /// <param name="parameter"></param>
        public override void Execute(object parameter)
        {

            System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog();
            var result = openFileDlg.ShowDialog();
            if (result.ToString() != string.Empty)
            {

                if (Directory.GetFiles(openFileDlg.SelectedPath, "*.png").Length == 0)
                {
                    if (Directory.GetFiles(openFileDlg.SelectedPath, "*.jpg").Length == 0)
                        MessageBox.Show("Non sono state trovate Immagini. Selezionare una nuova Cartella.");
                    else
                    {
                        _proctorInfoViewModel.TaskPath = openFileDlg.SelectedPath;
                    }
                }
                else
                {
                    _proctorInfoViewModel.TaskPath = openFileDlg.SelectedPath;
                }





                //_proctorInfoViewModel.TaskPath = openFileDlg.SelectedPath;
            }
        }
    }
}
