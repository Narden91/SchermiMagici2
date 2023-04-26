using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WpfApp1.Commands;
using WpfApp1.Services;
using WpfApp1.Stores;

namespace WpfApp1.ViewModels
{
    public class PatientInfoInputPageViewModel : ViewModelBase
    {
        private readonly ProctorStore _proctorStore;

        // Codice univoco paziente attuale
        private string _patientID;

        public string PatientID
        {
            get => _patientID;
        }

        // Nome della cartella in cui verrà salvato il paziente attuale
        private readonly string _folderCode;
        public string FolderCode
        {
            get => _folderCode;
        }

        // Cartella sorgente dei task da somministrare
        private readonly string _taskImageSourceFolder;
        public string TaskImageSourceFolder
        {
            get => _taskImageSourceFolder;
        }

        #region Campi relativi al paziente da inserire
        private string _name;
        private string _surname;
        private string _gender;
        private DateTime _birthdate;
        private string _dominantHand;
        private string _edClass;
        private string _diagnosedDiseases;
        private string _note;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(CanAddPatient));
            }
        }

        public string Surname
        {
            get => _surname;
            set
            {

                _surname = value;
                OnPropertyChanged(nameof(Surname));
                OnPropertyChanged(nameof(CanAddPatient));
            }
        }

        public string Gender
        {
            get => _gender;
            set
            {

                _gender = value;
                OnPropertyChanged(nameof(Gender));
                OnPropertyChanged(nameof(CanAddPatient));
            }
        }

        public DateTime Birthdate
        {
            get => _birthdate;
            set
            {
                _birthdate = value;
                OnPropertyChanged(nameof(Birthdate));
                OnPropertyChanged(nameof(CanAddPatient));
            }
        }

        public string DominantHand
        {
            get => _dominantHand;
            set
            {
                _dominantHand = value;

                OnPropertyChanged(nameof(DominantHand));
                OnPropertyChanged(nameof(CanAddPatient));

            }
        }

        public string EdClass
        {
            get => _edClass;
            set
            {
                _edClass = value;

                OnPropertyChanged(nameof(EdClass));
                OnPropertyChanged(nameof(CanAddPatient));

            }
        }

        public string DiagnosedDiseases
        {
            get => _diagnosedDiseases;
            set
            {
                _diagnosedDiseases = value;

                OnPropertyChanged(nameof(_diagnosedDiseases));
            }
        }

        public string Note
        {
            get => _note;
            set
            {
                if (_note == null || !_note.Equals(value))
                {
                    _note = value;
                    OnPropertyChanged(nameof(Note));
                }

            }
        }
        #endregion

        #region Check sul completamento dei campi
        public bool CanAddPatient =>
            HasName && HasSurname &&
            HasGender && HasEdClass && HasDominantHand;

        private bool HasName => !string.IsNullOrEmpty(Name);
        private bool HasSurname => !string.IsNullOrEmpty(Surname);
        private bool HasGender => !string.IsNullOrEmpty(Gender);
        private bool HasDominantHand => !string.IsNullOrEmpty(DominantHand);
        private bool HasEdClass => !string.IsNullOrEmpty(EdClass);
        #endregion

        #region Campi e Setter per valori comboboxes
        private readonly ObservableCollection<string> _genders;
        private readonly ObservableCollection<string> _hands;
        private readonly ObservableCollection<string> _classes;

        public IEnumerable<string> GenderList => _genders;
        public IEnumerable<string> DominantHandList => _hands;
        public IEnumerable<string> ClassList => _classes;
        #endregion

        public ICommand? AddPatientCommand { get; }

        /// <summary>
        /// Costruttore della classe che gestisce la View per l'inserimento delle informazioni del Paziente
        /// </summary>
        /// <param name="taskViewNavigationService"></param>
        /// <param name="patientStore"></param>
        /// <param name="proctorStore"></param>
        public PatientInfoInputPageViewModel(NavigationService taskViewNavigationService, PatientStore patientStore, ProctorStore proctorStore)
        {
            #region Inizializzazione valori Campi View
            _genders = new ObservableCollection<string>
            {
                "Maschile",
                "Femminile"
            };

            _hands = new ObservableCollection<string>
            {
                "Destra",
                "Sinistra"
            };

            _classes = new ObservableCollection<string>
            {
                "Terza Infanzia",
                "Prima Elementare",
                "Seconda Elementare",
                "Terza Elementare"
            };

            //_birthdate = DateTime.Today.AddDays(-1);
            _birthdate = new DateTime(2015, 01, 01);
            #endregion

            _proctorStore = proctorStore;
            _patientID = _proctorStore.IdPatientCodeGenerator();
            _folderCode = _proctorStore.UniqueCodeGenerator(PatientID);

            _taskImageSourceFolder = _proctorStore.GetTaskImagesPath();

            //MessageBox.Show(FolderCode, "Codice Folder");

            //MessageBox.Show(PatientID, "Codice Paziente");

            AddPatientCommand = new AddPatientCommand(this, taskViewNavigationService, patientStore);
        }
    }
}
