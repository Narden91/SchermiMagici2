using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.Stores;
using WpfApp1.ViewModels;

namespace WpfApp1.Commands
{
    public class AddProctorCommand : CommandBase
    {
        private readonly NavigationService _navigationService;
        private readonly ProctorPersonalInfoPageViewModel _proctorInfoViewModel;
        private readonly ProctorStore _proctorStore;

        public AddProctorCommand(ProctorPersonalInfoPageViewModel proctorInfoViewModel, NavigationService navigationService, ProctorStore proctorStore)
        {
            _proctorInfoViewModel = proctorInfoViewModel;
            _navigationService = navigationService;
            _proctorStore = proctorStore;
        }

        /// <summary>
        /// Funzione che esegue operazioni alla pressione del pulsante 
        /// AddProctorCommand nella View del Somministratore
        /// </summary>
        /// <param name="parameter"></param>
        public override void Execute(object parameter)
        {

            Proctor proctor = new Proctor()
            {
                Name = _proctorInfoViewModel.Name,
                Surname = _proctorInfoViewModel.Surname,
                City = _proctorInfoViewModel.City,
                TaskImagesPath = _proctorInfoViewModel.TaskPath,
                Notes = _proctorInfoViewModel.Notes
            };

            _proctorStore.CreateProctor(proctor);

            //MessageBox.Show( proctor.ToString(), "Successfully Added Proctor Information.");



            //_windowListImages = new WindowListImages(_proctorStore.GetTaskImagesPath());
            //_windowListImages.Show();



            _navigationService.Navigate();

        }
    }
}
