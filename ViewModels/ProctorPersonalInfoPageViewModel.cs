using System.Windows.Input;
using WpfApp1.Commands;
using WpfApp1.Services;
using WpfApp1.Stores;

namespace WpfApp1.ViewModels
{
    public class ProctorPersonalInfoPageViewModel : ViewModelBase
    {
        #region Fields
        private string _name;
        private string _surname;
        private string _city;
        private string _taskPath;
        private string _notes;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(CanAddProctor));
            }
        }

        public string Surname
        {
            get => _surname;
            set
            {
                _surname = value;
                OnPropertyChanged(nameof(Surname));
                OnPropertyChanged(nameof(CanAddProctor));
            }
        }

        public string City
        {
            get => _city;
            set
            {
                _city = value;
                OnPropertyChanged(nameof(City));
                OnPropertyChanged(nameof(CanAddProctor));
            }
        }

        public string TaskPath
        {
            get => _taskPath;
            set
            {
                _taskPath = value;
                OnPropertyChanged(nameof(TaskPath));
                OnPropertyChanged(nameof(CanAddProctor));
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged(nameof(Notes));
                OnPropertyChanged(nameof(CanAddProctor));
            }
        }
        #endregion

        #region Check sul completamento dei campi
        public bool CanAddProctor =>
            HasName && HasSurname && HasCity && HasTaskPath;

        private bool HasName => !string.IsNullOrEmpty(Name);
        private bool HasSurname => !string.IsNullOrEmpty(Surname);
        private bool HasCity => !string.IsNullOrEmpty(City);
        private bool HasTaskPath => !string.IsNullOrEmpty(TaskPath);
        #endregion

        public ICommand? AddProctorCommand { get; }

        public ICommand? AddPathTaskCommand { get; }

        public ProctorPersonalInfoPageViewModel(NavigationService patientInfoPageNavigationService, ProctorStore proctorStore)
        {
            AddProctorCommand = new AddProctorCommand(this, patientInfoPageNavigationService, proctorStore);
            AddPathTaskCommand = new AddPathTaskCommand(this, proctorStore);
        }

    }
}
