using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using WpfApp1.Commands;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.Stores;

namespace WpfApp1.ViewModels
{
    public class DevicePageViewModel : ViewModelBase
    {
        #region Fields
        private readonly PatientStore _patientStore;
        public string PatientExperimentOutputFolder => _patientStore.Patient.PatientOutputDataFolder;
        public string TaskSourceFolder => _patientStore.Patient.TaskPathFolder;
        public Patient Patient => _patientStore.Patient;
        #endregion

        #region Fields Wacom
        private readonly SynchronizationContext _synchronizationContext;
        private List<Wacom.Devices.IInkDeviceWatcher> _inkDeviceWatchers = new List<Wacom.Devices.IInkDeviceWatcher>();
        private ObservableCollection<Connection> _connectionList = new ObservableCollection<Connection>();

        public ObservableCollection<Connection> ConnectionList => _connectionList;

        private Connection _selectedDevice;

        public Connection SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                OnPropertyChanged(nameof(SelectedDevice));
            }
        }

        //public Connection SelectedDevice { get; set; }

        #endregion

        #region Comandi associati ai pulsanti
        public ICommand? StartTrialCommand { get; }
        public ICommand? BackToPatientInfoCommand { get; }
        #endregion

        /// <summary>
		/// Prelevo il nome del paziente per visualizzarlo nella UI
		/// </summary>
		public string PatientName
        {
            get => Patient.Name + " " + Patient.Surname;
        }

        public DevicePageViewModel(PatientStore patientStore, ExperimentStore experimentStore,
                                    DeviceConnectionStore deviceConnectionStore, NavigationService patientInfoPageNavigationService)
        {
            _patientStore = patientStore;

            _synchronizationContext = SynchronizationContext.Current;

            InitializeInkWatchers();

            StartTrialCommand = new StartTrialCommand(this, experimentStore, deviceConnectionStore, patientInfoPageNavigationService);
            //BackToPatientInfoCommand = new BackToPatientInfoCommand(this, patientInfoPageNavigationService);
        }


        #region InkWatchers Initializer
        /// <summary>
        /// Inizializza gli InkWatcher per poter connettere Dispositivi Wacom
        /// </summary>
        void InitializeInkWatchers()
        {
            StringBuilder sb = new StringBuilder();
            int countStarted = 0;
            for (int i = 0; i < 3; ++i)
            {
                try
                {
                    Wacom.Devices.IInkDeviceWatcher inkDeviceWatcher = i switch
                    {
                        0 => Wacom.Devices.InkDeviceWatcher.BLE,
                        1 => Wacom.Devices.InkDeviceWatcher.USB,
                        2 => Wacom.Devices.InkDeviceWatcher.WAC,
                        _ => throw new IndexOutOfRangeException()
                    };

                    _inkDeviceWatchers.Add(inkDeviceWatcher);
                    inkDeviceWatcher.DeviceAdded += (s, e) => _synchronizationContext.Post(o => DeviceAdded(s, e), null);
                    inkDeviceWatcher.DeviceRemoved += (s, e) => _synchronizationContext.Post(o => DeviceRemoved(s, e), null);
                    inkDeviceWatcher.Start();
                    ++countStarted;
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Unable to create InkDeviceWatcher [index={i}] {ex.Message}");

                    //Added 16/11/2022
                    sb.Clear();
                }
            }

            if (countStarted == 0)
            {
                sb.AppendLine("No Watchers have been started");
            }

            if (sb.Length > 0)
            {
                MessageBox.Show(sb.ToString());
            }
        }
        #endregion

        #region Helpers
        private Connection FindConnectionFromDeviceInfo(Wacom.Devices.IInkDeviceInfo e)
        {
            foreach (var i in _connectionList)
            {
                if (i.Id == e.Id)
                    return i;
            }
            return null;
        }
        #endregion

        #region DeviceWatcher Events
        /// <summary>
        /// Gestisce la rimozione di un Device Wacom
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceRemoved(object sender, Wacom.Devices.IInkDeviceInfo e)
        {
            var connection = FindConnectionFromDeviceInfo(e);
            if (connection != null)
            {
                _connectionList.Remove(connection);
                //connection.Close();
            }
        }

        /// <summary>
        /// Gestisce l'aggiunta di un Device Wacom
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceAdded(object sender, Wacom.Devices.IInkDeviceInfo e)
        {
            _connectionList.Add(new Connection(e));
        }
        #endregion

        #region Costruttore con ViewModel (Inutilizzato)
        //public DevicePageViewModel(NavigationService taskPageViewNavigationService, PatientStore patientStore, 
        //                           ProctorStore proctorStore, ExperimentStore experimentStore, DeviceConnectionStore deviceConnectionStore)
        //{

        //    //Debug per navigare senza utilizzare tavoletta
        //    //StartTrialCommand = new NavigateCommand(taskPageViewNavigationService);

        //    _patientStore = patientStore;

        //    _synchronizationContext = SynchronizationContext.Current;

        //    InitializeInkWatchers();

        //    StartTrialCommand = new StartTrialCommand(this, taskPageViewNavigationService, experimentStore, deviceConnectionStore);
        //}
        #endregion

    }
}
