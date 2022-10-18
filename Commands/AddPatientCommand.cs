using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.Stores;
using WpfApp1.ViewModels;


namespace WpfApp1.Commands
{
    public class AddPatientCommand : CommandBase
    {
        private readonly NavigationService _navigationService;
        private readonly PatientInfoInputPageViewModel _patientInfoViewModel;
        private readonly PatientStore _patientStore;

        public AddPatientCommand(PatientInfoInputPageViewModel patientInfoViewModel, NavigationService navigationService, PatientStore patientStore)
        {
            _patientInfoViewModel = patientInfoViewModel;
            _navigationService = navigationService;
            _patientStore = patientStore;
        }

        /// <summary>
        /// Funzione che esegue operazioni alla pressione del pulsante 
        /// AddPatientCommand nella View per l'inserimento delle informazioni del Paziente
        /// </summary>
        /// <param name="parameter"></param>
        public override void Execute(object parameter)
        {

            Patient patient = new Patient()
            {
                Name = _patientInfoViewModel.Name,
                Surname = _patientInfoViewModel.Surname,
                Gender = _patientInfoViewModel.Gender,
                Birthdate = _patientInfoViewModel.Birthdate.ToString("dd/MM/yyyy"),
                Dominanthand = _patientInfoViewModel.DominantHand,
                Edclass = _patientInfoViewModel.EdClass,
                DiagnosedDiseases = _patientInfoViewModel.DiagnosedDiseases,
                Notes = _patientInfoViewModel.Note
            };

            // Ho il paziente registrato con tutte le sue informazioni e 
            // Il folder nel quale salvare i task
            _patientStore.CreatePatient(patient);
            _patientStore.SetPatientFolder(_patientInfoViewModel.FolderCode);

            // Cartella sorgente dei task
            _patientStore.SetTaskSourceFolder(_patientInfoViewModel.TaskImageSourceFolder);

            // Set del codiceID del paziente
            _patientStore.SetPatientID(_patientInfoViewModel.PatientID);

            //MessageBox.Show(_patientStore.PrintPatient(), "Successfully Added Patient." );

            _navigationService.Navigate();

        }
    }
}
