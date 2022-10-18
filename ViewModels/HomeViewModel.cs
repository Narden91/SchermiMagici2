using System.Windows.Input;
using WpfApp1.Commands;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public ICommand? StartApplicationCommand { get; }

        public HomeViewModel(NavigationService proctorPageNavigationService)
        {
            StartApplicationCommand = new NavigateCommand(proctorPageNavigationService);
        }

    }
}
