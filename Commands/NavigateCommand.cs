using WpfApp1.Services;

namespace WpfApp1.Commands
{
    public class NavigateCommand : CommandBase
    {
        private readonly NavigationService _navigationService;

        public NavigateCommand(NavigationService navigationService)
        {
            _navigationService = navigationService;
        }


        /// <summary>
        /// Responsible of the view change in the application 
        /// </summary>
        /// <param name="parameter"></param>
        public override void Execute(object parameter)
        {

            //MessageBox.Show(App.GetAppFolder());

            _navigationService.Navigate();
        }
    }
}
