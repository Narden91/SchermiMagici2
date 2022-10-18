using System;
using WpfApp1.Services;
using WpfApp1.ViewModels;

namespace WpfApp1.Commands
{
    public class BackToPatientInfoCommand : CommandBase
    {
        private readonly NavigationService _navigationService;
        private readonly DevicePageViewModel _devicePageViewModel;

        public BackToPatientInfoCommand(DevicePageViewModel devicePageViewModel, NavigationService navigationService)
        {
            _devicePageViewModel = devicePageViewModel;
            _navigationService = navigationService;
        }

        public override void Execute(object parameter)
        {
            // Torno alla schermata di inserimento di informazioni del nuovo paziente

            // Elimina memoria inutilizzata
            GC.Collect();

            _navigationService.Navigate();
        }
    }

}
