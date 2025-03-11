using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfApp1.Stores;
using WpfApp1.Services;
using WPFCustomMessageBox;






namespace WpfApp1
{

    /// <summary>
    /// Interaction logic for DeviceWindow.xaml
    /// </summary>
    public partial class DeviceWindow : Window, INotifyPropertyChanged
    {
        private readonly Wacom.Devices.IInkDeviceInfo _inkDeviceInfo;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly object _penDataLock = new object();
        private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private readonly ExperimentStore _experimentStore;
        private readonly Services.BackupService _backupService;
        private TaskCanvas _taskCanvasWindow;

        private List<TaskCanvas> _taskCanvasWindows = new List<TaskCanvas>();


        #region Attributi
        private string _pathCurrentTask;
        private string _instructionBoxText;
        private string _pathTextCurrentTask;
        private string _imageSavingFolderPath;



        private bool _isInstructionFolderEmpty = true;

        /// <summary>
        /// Path dell'immagine Task da somministrare
        /// </summary>
        public string PathCurrentTask
        {
            get => _pathCurrentTask;
            set
            {
                _pathCurrentTask = value;
            }
        }

        /// <summary>
        /// Path del testo istruzioni per il Task da somministrare
        /// </summary>
        public string PathTextCurrentTask
        {
            get => _pathTextCurrentTask;
            set
            {
                _pathTextCurrentTask = value;
            }
        }

        /// <summary>
        /// Gestione del contenuto della Textbox di Istruzioni
        /// </summary>
        public string InstructionBoxText
        {
            get => _instructionBoxText;
            set
            {
                _instructionBoxText = value;
            }
        }

        public string TaskNameToShowToUI => Path.GetFileNameWithoutExtension(_pathCurrentTask);

        // Indice per scorrere tutte le immagini dei task da sottoporre
        private int _taskCounter = 0;

        // Indice che viene incrementato solo se sono in presenza di un Task da registrare e per 
        // poter costruire il file .csv dello specifico Task
        private int _trueTaskIndex = 1;

        private int _numberCsvFile = 0;

        public string NumberCsvFile => _numberCsvFile.ToString();

        private int _totalNumberOfTask = 0;

        public string TotalExperimentTasks => _totalNumberOfTask.ToString();

        /// <summary>
        /// Prelevo il nome del paziente per visualizzarlo nella UI
        /// </summary>
        public string PatientName
        {
            get => _experimentStore.GetCurrentPatient();
        }

        /// <summary>
        /// Codice identificativo del Paziente
        /// </summary>
        public string PatientId
        {
            get => _experimentStore.GetPatientID();
        }

        /// <summary>
        /// Path della Cartella in cui verranno salvati i vari .csv del paziente
        /// </summary>
        public string DataPath
        {
            get => _experimentStore.ExperimentFolder();
        }

        /// <summary>
        /// Lista dei vari path delle immagini contenute nella cartella
        /// sorgente per l'esperimento
        /// </summary>
        public List<string> TaskImageList
        {
            get => _experimentStore.GetListTaskImage();
        }


        /// <summary>
        /// Lista dei file txt relativi ai vari task
        /// </summary>
        public List<string> TaskTextList
        {
            get => _experimentStore.GetListTaskInstruction();
        }

        private Wacom.Devices.IDigitalInkDevice _digitalInkDevice;
        private Wacom.Devices.IRealTimeInkService _realTimeInkService;

        private Wacom.Devices.IInkDeviceNotification<Wacom.Devices.BatteryStateChangedEventArgs> _batteryStatedChangedNotification;
        #endregion

        public DeviceWindow(Wacom.Devices.IInkDeviceInfo inkDeviceInfo, ExperimentStore experimentStore)
        {
            _inkDeviceInfo = inkDeviceInfo;
            _experimentStore = experimentStore;

            // Prelevo il Path dell'immagine per il primo Task (_taskCounter è incrementato di volta in volta)
            PathCurrentTask = TaskImageList[_taskCounter];

            // Ottengo il numero dei Task dell'esperimento
            _totalNumberOfTask = TaskImageList.Count;

            if (TaskTextList.Count > 0)
            {
                _isInstructionFolderEmpty = false;

                PathTextCurrentTask = TaskTextList[_taskCounter];
                InstructionBoxText = "";
                InstructionBoxText = File.ReadAllText(PathTextCurrentTask);
            }
            else
                InstructionBoxText = "Nessun file di Istruzioni trovato.";

            _imageSavingFolderPath = Path.Combine(DataPath, "Images");

            // Crea la Cartella in Documenti/Application_saving_folder/..
            Directory.CreateDirectory(_imageSavingFolderPath);

            // Initialize the backup service with experiment folder path
            _backupService = new Services.BackupService(DataPath);

            // Salvataggio anagrafica paziente
            SavePatientInformation();

            _synchronizationContext = SynchronizationContext.Current;

            InitializeComponent();

            Initialize_DeviceProperties();

            Title = Title + " - " + inkDeviceInfo.DeviceName;

            Loaded += (s, e) => Task.Run(async () => await ConnectAsync().ConfigureAwait(continueOnCapturedContext: false), _cancellationToken.Token);
            Unloaded += (s, e) => Disconnect();
            Closed += (s, e) => Disconnect();
        }


        //if (TaskNameToShowToUI == "Task_6" || TaskNameToShowToUI == "Task_7" || TaskNameToShowToUI == "Task_7")
        //	RealTimeInk_StartStop = false;
        //else
        //             RealTimeInk_StartStop = true;

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenTaskWindow(object sender, RoutedEventArgs e)
        {
            string imageTaskPath = Path.Combine(_imageSavingFolderPath, TaskNameToShowToUI + ".png");

            RealTimeInk_StartStop = true;
            NewTaskStartButtonEnabled = false;
            SaveTaskButtonEnabled = true;
            SkipTaskButtonEnabled = false;

            // Start the backup process with the current task name
            _backupService.StartBackup(TaskNameToShowToUI, () => {
                lock (_penDataLock)
                {
                    return new List<Wacom.Devices.RealTimePointReceivedEventArgs>(_realTimeInk_PenData);
                }
            });

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                TaskCanvas newCanvas = new TaskCanvas(PathCurrentTask, imageTaskPath);
                _taskCanvasWindows.Add(newCanvas);
                newCanvas.Show();
            }));
        }


        ///// <summary>
        ///// Funzione collegata al pulsante Open nella View
        ///// Apre la finestra del task i-esimo in un nuovo thread
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void OpenTaskWindow(object sender, RoutedEventArgs e)
        //{

        //    string imageTaskPath = Path.Combine(_imageSavingFolderPath, TaskNameToShowToUI + ".png");

        //    RealTimeInk_StartStop = true;
        //    NewTaskStartButtonEnabled = false;
        //    SaveTaskButtonEnabled = true;
        //    SkipTaskButtonEnabled = false;

        //    //MessageBox.Show(imageTaskPath, "Path Immagine Task ");


        //    //Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
        //    //{
        //    //	_taskCanvasWindow = new TaskCanvas(PathCurrentTask, imageTaskPath);
        //    //	_taskCanvasWindow.Show();
        //    //}));

        //    //Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
        //    //{
        //    //    _taskCanvasWindow = new TaskCanvas(PathCurrentTask, imageTaskPath);
        //    //    _taskCanvasWindow.Closed += (s, e) => _openTaskCanvasWindows.Remove((TaskCanvas)s);
        //    //    _openTaskCanvasWindows.Add(_taskCanvasWindow);
        //    //    _taskCanvasWindow.Show();
        //    //}));

        //    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
        //    {
        //        TaskCanvas newCanvas = new TaskCanvas(PathCurrentTask, imageTaskPath);
        //        _taskCanvasWindows.Add(newCanvas);
        //        newCanvas.Show();
        //    }));

        //    #region Threading usando Task
        //    //Task.Factory.StartNew(new Action(() =>
        //    //{
        //    //	_taskCanvasWindow = new TaskCanvas(_pathCurrentTask);
        //    //	_taskCanvasWindow.Show();
        //    //}), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        //    #endregion
        //}

        /// <summary>
        /// Funzione per salvare le informazioni del paziente in un 
        /// file .txt
        /// </summary>
        private void SavePatientInformation()
        {
            string registryFilename = "Anagrafica_" + PatientId + ".txt";

            string registryFilePath = Path.Combine(DataPath, registryFilename);

            try
            {
                File.WriteAllText(@registryFilePath, _experimentStore.PrintExperimentPatient());
            }
            catch (Exception ex)
            {
                _synchronizationContext.Post(o => MessageBox.Show($"Unable to create Patient Information on a .txt file"), null);
            }

        }

        private async Task ConnectAsync()
        {
            try
            {
                DeviceStatus = _inkDeviceInfo.TransportProtocol
                             == Wacom.Devices.TransportProtocol.BLE ?
                             "Loading (you may need to press the device's centre button)..." : "Loading...";

                _digitalInkDevice = await Wacom.Devices.InkDeviceFactory.Instance.CreateDeviceAsync(
                                    _inkDeviceInfo, App.AppId, true, false, OnDeviceStatusChanged, PairingModeEnabledCallback).
                                    ConfigureAwait(continueOnCapturedContext: false);

                DeviceStatus = "Connected";

                GetNotifications();
                await GetServicesAsync().ConfigureAwait(continueOnCapturedContext: false);
                await QueryDevicePropertiesAsync().ConfigureAwait(continueOnCapturedContext: false);

                async Task GetServicesAsync()
                {
                    try
                    {
                        _realTimeInkService = _digitalInkDevice.GetService(Wacom.Devices.InkDeviceService.RealTimeInk) as Wacom.Devices.IRealTimeInkService;
                        if (_realTimeInkService != null)
                        {
                            Initialize_RealTimeInk();
                        }
                    }
                    catch (Wacom.Devices.LicensingException)
                    {
                    }

                }

                void GetNotifications()
                {
                    _batteryStatedChangedNotification = _digitalInkDevice.GetNotification<Wacom.Devices.BatteryStateChangedEventArgs>(Wacom.Devices.Notification.Device.BatteryStateChanged);
                    if (_batteryStatedChangedNotification != null)
                    {
                        _batteryStatedChangedNotification.Notification += OnBatteryStatedChanged_Notification;
                    }
                }

            }
            catch (Exception ex)
            {
                DeviceStatus = $"Connect failed: {ex.Message}";
            }
        }

        private async Task<bool> PairingModeEnabledCallback(bool isAuthorized)
        {
            return await Task.Run(() =>
            {
                SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
                var result = MessageBox.Show(messageBoxText: $"PairingModeEnabled.isAuthorized={isAuthorized}. Return true=yes, false=no", caption: "Pairing Mode", button: MessageBoxButton.YesNo);
                return result == MessageBoxResult.Yes;
            }, _cancellationToken.Token).ConfigureAwait(continueOnCapturedContext: false);
        }


        private void Disconnect()
        {
            _cancellationToken.Cancel();

            if (_batteryStatedChangedNotification != null)
            {
                _batteryStatedChangedNotification.Notification -= OnBatteryStatedChanged_Notification;
                _batteryStatedChangedNotification = null;
            }

            if (_digitalInkDevice != null)
            {
                try
                {
                    _digitalInkDevice.Close();
                }
                finally
                {
                    _digitalInkDevice.Dispose();
                    _digitalInkDevice = null;
                }
            }
        }

        #region Funzioni Handler eventi sulle proprietà della UI

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChangedSync(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnPropertyChanged(string propertyName)
        {
            _synchronizationContext.Post(o => OnPropertyChangedSync(propertyName), null);
        }
        #endregion

        #region Notification Handling
        private void OnBatteryStatedChanged_Notification(object sender, Wacom.Devices.BatteryStateChangedEventArgs e)
        {
        }
        #endregion

        #region DeviceWindow (Status)

        private string _deviceStatus;
        public string DeviceStatus
        {
            get => _deviceStatus;
            private set
            {
                _deviceStatus = value;
                OnPropertyChanged(nameof(DeviceStatus));
            }
        }

        private void OnDeviceStatusChanged(object sender, Wacom.Devices.DeviceStatusChangedEventArgs e)
        {
            DeviceStatus = e.Status.ToString();
        }
        #endregion

        #region Device Info 
        public string DeviceInfo_Id => _inkDeviceInfo.Id;
        public string DeviceInfo_TransportProtocol => _inkDeviceInfo.TransportProtocol.ToString();
        public string DeviceInfo_DeviceModel => _inkDeviceInfo.DeviceModel.ToString();
        public string DeviceInfo_DeviceName => _inkDeviceInfo.DeviceName;
        public string DeviceInfo_Stream
        {
            get
            {
                using var stream = new MemoryStream();
                _inkDeviceInfo.ToStream(stream);
                var data = stream.ToArray();
                return System.Text.Encoding.UTF8.GetString(data);
            }
        }

        private void DeviceInfo_Stream_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Save Stream",
                AddExtension = true,
                DefaultExt = "stream",
                Filter = "Streams|*.stream|All|*.*",
                OverwritePrompt = true
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using var stream = new FileStream(saveFileDialog.FileName, FileMode.Create);
                _inkDeviceInfo.ToStream(stream);
            }
        }
        #endregion

        #region Device Properties 

        private ObservableCollection<DevicePropertyValue> _deviceProperties = new ObservableCollection<DevicePropertyValue>();

        public ObservableCollection<DevicePropertyValue> DeviceWindow_DeviceProperties => _deviceProperties;

        private void Initialize_DeviceProperties()
        {
            foreach (var i in DeviceProperties.Device) _deviceProperties.Add(new DevicePropertyValue(i));
            foreach (var i in DeviceProperties.Digitizer) _deviceProperties.Add(new DevicePropertyValue(i));
            foreach (var i in DeviceProperties.Screen) _deviceProperties.Add(new DevicePropertyValue(i));
            foreach (var i in DeviceProperties.STU) _deviceProperties.Add(new DevicePropertyValue(i));
            foreach (var i in DeviceProperties.SmartPad) _deviceProperties.Add(new DevicePropertyValue(i));
            foreach (var i in DeviceProperties.WacomDriver) _deviceProperties.Add(new DevicePropertyValue(i));
        }


        private async Task QueryDevicePropertiesAsync()
        {
            foreach (var i in _deviceProperties)
            {
                _cancellationToken.Token.ThrowIfCancellationRequested();
                try
                {
                    i.SetValue(await _digitalInkDevice.GetPropertyAsync(i.Name, _cancellationToken.Token).ConfigureAwait(continueOnCapturedContext: false));
                }
                catch
                {
                }
            }
        }

        #endregion

        #region RealTimeInk 

        #region Attributi privati gestione punti acquisiti
        private List<Wacom.Devices.RealTimePointReceivedEventArgs> _realTimeInk_PenData = new List<Wacom.Devices.RealTimePointReceivedEventArgs>();
        private Wacom.Devices.RealTimePointReceivedEventArgs? _realTimeInk_PenData_Last;
        #endregion

        private void Initialize_RealTimeInk()
        {
            _synchronizationContext.Post(RealTimeInk_InitializeSync, null);

            void RealTimeInk_InitializeSync(object _)
            {
                OnPropertyChangedSync(nameof(RealTimeInk_IsStarted));
            }
        }

        public bool RealTimeInk_StartStop
        {
            get => _realTimeInkService?.IsStarted ?? false;
            set
            {
                Task.Run(async () =>
                {
                    if (value)
                    {
                        _realTimeInk_PenData.Clear();
                        _realTimeInk_PenData_Last = null;
                        _realTimeInkService.PointReceived += _realTimeInkService_PointReceived;
                        await _realTimeInkService.StartAsync(_cancellationToken.Token).ConfigureAwait(continueOnCapturedContext: false);
                    }
                    else
                    {
                        _realTimeInk_PenData_Last = null;
                        await _realTimeInkService.StopAsync(_cancellationToken.Token).ConfigureAwait(continueOnCapturedContext: false);
                        _realTimeInkService.PointReceived -= _realTimeInkService_PointReceived;
                    }
                    OnPropertyChanged(nameof(RealTimeInk_StartStop));
                    OnPropertyChanged(nameof(RealTimeInk_IsStarted));
                    OnPenDataPropertyChanged();
                }, _cancellationToken.Token);
            }
        }


        /// <summary>
        /// Operatore null-coalescing ?? restituisce il valore dell'operando a sinistra se è null; in caso contrario, 
        /// valuta l'operando a destra e ne restituisce il risultato. L'operatore ?? non valuta l'operando di destra 
        /// se l'operando di sinistra restituisce un valore non Null.
        /// </summary>
        //public string RealTimeInk_IsStarted => (_realTimeInkService?.IsStarted ?? false) ? "YES" : "NO";

        public string RealTimeInk_IsStarted => (_realTimeInkService?.IsStarted ?? false) ? "SI" : "NO";



        //// Funzione per effettuare il refresh della UI 
        //private void OnPenDataPropertyChanged()
        //{
        //    _synchronizationContext.Post(OnPenDataPropertyChangedSync, null);

        //    void OnPenDataPropertyChangedSync(object _) // This must be called on the UI thread.
        //    {
        //        var last = _realTimeInk_PenData_Last;

        //        if (PropertyChanged != null)
        //        {
        //            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(SaveTaskButtonEnabled)));
        //            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(SkipTaskButtonEnabled)));
        //            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_Count_int)));
        //            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(NumberCsvFile)));
        //            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(NewTaskStartButtonEnabled)));
        //            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(PathCurrentTask)));
        //            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(TaskNameToShowToUI)));
        //            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(InstructionBoxText)));
        //        }
        //    }
        //}

        // Funzione per effettuare il refresh della UI in modo thread-safe
        private void OnPenDataPropertyChanged()
        {
            _synchronizationContext.Post(OnPenDataPropertyChangedSync, null);

            void OnPenDataPropertyChangedSync(object _) // This must be called on the UI thread.
            {
                Wacom.Devices.RealTimePointReceivedEventArgs? last;

                // Thread-safe access to the last point
                lock (_penDataLock)
                {
                    last = _realTimeInk_PenData_Last;
                }

                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(SaveTaskButtonEnabled)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(SkipTaskButtonEnabled)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_Count_int)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(NumberCsvFile)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(NewTaskStartButtonEnabled)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(PathCurrentTask)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(TaskNameToShowToUI)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(InstructionBoxText)));

                    // Update point-specific properties if a point is available
                    if (last != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_Timestamp)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_Point)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_Phase)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_Pressure)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_PointDisplay)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_PointRaw)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_PressureRaw)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_TimestampRaw)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_Sequence)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_Rotation)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_Azimuth)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_Altitude)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_Tilt)));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_PenId)));
                    }
                }
            }
        }


        /// <summary>
        /// Verifies if a CSV file exists and has valid content
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <returns>A tuple containing existence status, file size, and any error message</returns>
        private (bool exists, long fileSize, string message) VerifyCsvFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return (false, 0, "Il percorso del file non è valido");
                }

                if (!Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, 0, "Il file deve avere estensione .csv");
                }

                if (!File.Exists(filePath))
                {
                    return (false, 0, "Il file non esiste");
                }

                // Get file info for additional verification
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    return (true, 0, "Il file esiste ma è vuoto");
                }

                // Verify file can be opened and has valid structure
                try
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        // Read the header to verify basic structure
                        string header = reader.ReadLine();
                        if (string.IsNullOrEmpty(header) || !header.Contains("Timestamp") || !header.Contains("Point"))
                        {
                            return (true, fileInfo.Length, "Il file non ha un'intestazione valida");
                        }
                    }
                }
                catch (IOException ioEx)
                {
                    return (true, fileInfo.Length, $"Impossibile leggere il file: {ioEx.Message}");
                }

                return (true, fileInfo.Length, "File valido");
            }
            catch (Exception ex)
            {
                return (false, 0, $"Errore durante la verifica del file: {ex.Message}");
            }
        }


        //private void _realTimeInkService_PointReceived(object sender, Wacom.Devices.RealTimePointReceivedEventArgs e)
        //{
        //    _realTimeInk_PenData_Last = e;
        //    _realTimeInk_PenData.Add(_realTimeInk_PenData_Last);
        //    OnPenDataPropertyChanged();
        //}

        //private void _realTimeInkService_PointReceived(object sender, Wacom.Devices.RealTimePointReceivedEventArgs e)
        //{
        //    // Create a copy of the event args to prevent any external modification
        //    var pointCopy = e;

        //    // Use lock to ensure thread-safe access to the collection
        //    lock (_realTimeInk_PenData)
        //    {
        //        _realTimeInk_PenData_Last = pointCopy;
        //        _realTimeInk_PenData.Add(pointCopy);
        //    }

        //    // Update UI with the latest point count
        //    OnPenDataPropertyChanged();
        //}

        private void _realTimeInkService_PointReceived(object sender, Wacom.Devices.RealTimePointReceivedEventArgs e)
        {
            // Use lock to ensure thread-safe access to the collection
            lock (_penDataLock)
            {
                _realTimeInk_PenData_Last = e;
                _realTimeInk_PenData.Add(e);
            }

            // Update UI with the latest point count
            OnPenDataPropertyChanged();
        }

        /// <summary>
        /// Funzione per mostrare sulla UI il numero di punti acquisiti
        /// </summary>
        public int RealTimeInk_Count_int => _realTimeInk_PenData.Count;

        /// <summary>
        /// Funzione Per gestire l'abilitazione del pulsante di Salvataggio
        /// </summary>
        //public bool RealTimeInk_Count => ((_realTimeInk_PenData.Count > 0) && (TaskNameToShowToUI != "Task_7") && (TaskNameToShowToUI != "Task_6"));

        #region Pulsante Salva Task

        private bool _saveTaskButtonEnabled = false;

        /// <summary>
        /// Funzione Per gestire l'abilitazione del pulsante Salva
        /// </summary> 
        public bool SaveTaskButtonEnabled
        {
            get => _saveTaskButtonEnabled;
            set
            {
                _saveTaskButtonEnabled = value;
            }
        }
        #endregion


        #region Pulsante Salta Task

        private bool _skipTaskButtonEnabled = true;

        /// <summary>
        /// Funzione Per gestire l'abilitazione del pulsante Avanti
        /// </summary> 
        public bool SkipTaskButtonEnabled
        {
            get => _skipTaskButtonEnabled;
            set
            {
                _skipTaskButtonEnabled = value;
            }
        }
        #endregion

        #region Pulsante Somministra Task

        private bool _newTaskStartButtonEnabled = true;

        /// <summary>
        /// Condizione abilitazione pulsante Verde di Somministrazione Task
        /// </summary>
        public bool NewTaskStartButtonEnabled
        {
            get => _newTaskStartButtonEnabled;
            set
            {
                _newTaskStartButtonEnabled = value;
            }
        }
        #endregion

        private void RealTimeInk_PenData_Clear(object sender, RoutedEventArgs e)
        {
            _realTimeInk_PenData_Last = null;
            _realTimeInk_PenData.Clear();
            OnPenDataPropertyChanged();
        }


        ///// <summary>
        ///// Funzione responsabile della formattazione del salvataggio dei dati acquisiti
        ///// </summary>
        ///// <param name="fileName"></param>
        //private async Task RealTimeInk_SavePenData(string fileName)
        //{
        //	try
        //	{
        //		using var stream = File.CreateText(fileName);
        //		stream.WriteLine("Timestamp,PointX,PointY,Phase,Pressure,PointDisplayX,PointDisplayY,PointRawX,PointRawY,PressureRaw,TimestampRaw,Sequence,Rotation,Azimuth,Altitude,TiltX,TiltY,PenId");

        //		StringBuilder sb = new StringBuilder();
        //		foreach (var item in _realTimeInk_PenData)
        //		{
        //			sb.Append($"{item.Timestamp.ToString("O")},{item.Point.X,6},{item.Point.Y,6},{item.Phase.ToString(),-11}");
        //			sb.Append(item.Pressure.HasValue ? $",{item.Pressure.Value,9}" : ",");
        //			sb.Append(item.PointDisplay.HasValue ? $",{item.PointDisplay.Value.X,6},{item.PointDisplay.Value.Y,6}" : ",,");
        //			sb.Append(item.PointRaw.HasValue ? $",{item.PointRaw.Value.X,6},{item.PointRaw.Value.Y,6}" : ",,");
        //			sb.Append(item.PressureRaw.HasValue ? $",{item.PressureRaw.Value,6}" : ",");
        //			sb.Append(item.TimestampRaw.HasValue ? $",{item.TimestampRaw.Value,8}" : ",");
        //			sb.Append(item.Sequence.HasValue ? $",{item.Sequence.Value,8}" : ",");
        //			sb.Append(item.Rotation.HasValue ? $",{item.Rotation.Value,9}" : ",");
        //			sb.Append(item.Azimuth.HasValue ? $",{item.Azimuth.Value,9}" : ",");
        //			sb.Append(item.Altitude.HasValue ? $",{item.Altitude.Value,9}" : ",");
        //			sb.Append(item.Tilt.HasValue ? $",{item.Tilt.Value.X,9},{item.Tilt.Value.Y,9}" : ",,");
        //			sb.Append(item.PenId.HasValue ? $",0x{item.PenId.Value:x8}" : ",");
        //			stream.WriteLine(sb.ToString());
        //			sb.Clear();
        //		}
        //		stream.Close();
        //	}
        //	catch (Exception ex)
        //	{
        //		_synchronizationContext.Post(o => MessageBox.Show($"Unable to load image: {ex.Message}"), null);
        //	}
        //}

        /// <summary>
        /// Esegue il salvataggio dei dati rilevati dalla penna del device utilizzato
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        //private bool SavePenData(string fileName)
        //{
        //    bool status = false;
        //    try
        //    {
        //        using var stream = File.CreateText(fileName);
        //        stream.WriteLine("Timestamp,PointX,PointY,Phase,Pressure,PointDisplayX,PointDisplayY,PointRawX,PointRawY,PressureRaw,TimestampRaw,Sequence,Rotation,Azimuth,Altitude,TiltX,TiltY,PenId");

        //        StringBuilder sb = new StringBuilder();
        //        foreach (var item in _realTimeInk_PenData)
        //        {
        //            sb.Append($"{item.Timestamp.ToString("O")},{item.Point.X,6},{item.Point.Y,6},{item.Phase.ToString(),-11}");
        //            sb.Append(item.Pressure.HasValue ? $",{item.Pressure.Value,9}" : ",");
        //            sb.Append(item.PointDisplay.HasValue ? $",{item.PointDisplay.Value.X,6},{item.PointDisplay.Value.Y,6}" : ",,");
        //            sb.Append(item.PointRaw.HasValue ? $",{item.PointRaw.Value.X,6},{item.PointRaw.Value.Y,6}" : ",,");
        //            sb.Append(item.PressureRaw.HasValue ? $",{item.PressureRaw.Value,6}" : ",");
        //            sb.Append(item.TimestampRaw.HasValue ? $",{item.TimestampRaw.Value,8}" : ",");
        //            sb.Append(item.Sequence.HasValue ? $",{item.Sequence.Value,8}" : ",");
        //            sb.Append(item.Rotation.HasValue ? $",{item.Rotation.Value,9}" : ",");
        //            sb.Append(item.Azimuth.HasValue ? $",{item.Azimuth.Value,9}" : ",");
        //            sb.Append(item.Altitude.HasValue ? $",{item.Altitude.Value,9}" : ",");
        //            sb.Append(item.Tilt.HasValue ? $",{item.Tilt.Value.X,9},{item.Tilt.Value.Y,9}" : ",,");
        //            sb.Append(item.PenId.HasValue ? $",0x{item.PenId.Value:x8}" : ",");
        //            stream.WriteLine(sb.ToString());
        //            sb.Clear();
        //            status = true;
        //        }
        //        stream.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Errore: {ex.Message}");
        //    }
        //    return status;
        //}

        /// <summary>
        /// Salva i dati acquisiti dalla penna in un file CSV con meccanismi di verifica e sicurezza
        /// </summary>
        /// <param name="fileName">Il percorso completo del file</param>
        /// <returns>Tuple containing success status, points saved, and message</returns>
        private (bool success, int pointsSaved, string message) SavePenData(string fileName)
        {
            List<Wacom.Devices.RealTimePointReceivedEventArgs> dataCopy;

            // Safely get a copy of the data to avoid race conditions
            lock (_penDataLock)
            {
                if (_realTimeInk_PenData == null || _realTimeInk_PenData.Count == 0)
                {
                    return (false, 0, "Nessun dato da salvare");
                }

                // Create a copy of the data to operate on
                dataCopy = new List<Wacom.Devices.RealTimePointReceivedEventArgs>(_realTimeInk_PenData);
            }

            // Now work with the copy safely outside the lock
            int expectedRowCount = dataCopy.Count;

            try
            {
                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create a temp file first for safe writing
                string tempFileName = Path.Combine(
                    Path.GetDirectoryName(fileName) ?? "",
                    Path.GetFileNameWithoutExtension(fileName) + ".temp.csv");

                int rowCount = 0;

                using (var stream = new StreamWriter(tempFileName, false, Encoding.UTF8))
                {
                    // Write the header
                    stream.WriteLine("Timestamp,PointX,PointY,Phase,Pressure,PointDisplayX,PointDisplayY,PointRawX,PointRawY,PressureRaw,TimestampRaw,Sequence,Rotation,Azimuth,Altitude,TiltX,TiltY,PenId");

                    StringBuilder sb = new StringBuilder(200); // Pre-allocate reasonable capacity

                    foreach (var item in dataCopy)
                    {
                        sb.Clear();
                        sb.Append($"{item.Timestamp.ToString("O")},{item.Point.X,6},{item.Point.Y,6},{item.Phase.ToString(),-11}");
                        sb.Append(item.Pressure.HasValue ? $",{item.Pressure.Value,9}" : ",");
                        sb.Append(item.PointDisplay.HasValue ? $",{item.PointDisplay.Value.X,6},{item.PointDisplay.Value.Y,6}" : ",,");
                        sb.Append(item.PointRaw.HasValue ? $",{item.PointRaw.Value.X,6},{item.PointRaw.Value.Y,6}" : ",,");
                        sb.Append(item.PressureRaw.HasValue ? $",{item.PressureRaw.Value,6}" : ",");
                        sb.Append(item.TimestampRaw.HasValue ? $",{item.TimestampRaw.Value,8}" : ",");
                        sb.Append(item.Sequence.HasValue ? $",{item.Sequence.Value,8}" : ",");
                        sb.Append(item.Rotation.HasValue ? $",{item.Rotation.Value,9}" : ",");
                        sb.Append(item.Azimuth.HasValue ? $",{item.Azimuth.Value,9}" : ",");
                        sb.Append(item.Altitude.HasValue ? $",{item.Altitude.Value,9}" : ",");
                        sb.Append(item.Tilt.HasValue ? $",{item.Tilt.Value.X,9},{item.Tilt.Value.Y,9}" : ",,");
                        sb.Append(item.PenId.HasValue ? $",0x{item.PenId.Value:x8}" : ",");
                        stream.WriteLine(sb.ToString());
                        rowCount++;
                    }
                }

                // Verify data integrity - check row count
                if (rowCount != expectedRowCount)
                {
                    // Delete temp file and return failure
                    if (File.Exists(tempFileName))
                    {
                        File.Delete(tempFileName);
                    }
                    return (false, rowCount, $"Verifica dei dati fallita: {rowCount} righe salvate, {expectedRowCount} attese");
                }

                // Create a backup of the existing file if it exists
                if (File.Exists(fileName))
                {
                    string backupFileName = fileName + ".bak";
                    if (File.Exists(backupFileName))
                    {
                        File.Delete(backupFileName);
                    }
                    File.Move(fileName, backupFileName);
                }

                // Move temp file to final destination safely
                File.Move(tempFileName, fileName);

                // Final verification
                var (exists, fileSize, verificationMessage) = VerifyCsvFile(fileName);
                if (!exists || fileSize == 0)
                {
                    return (false, rowCount, verificationMessage);
                }

                return (true, rowCount, $"File salvato correttamente con {rowCount} punti");
            }
            catch (Exception ex)
            {
                return (false, 0, $"Errore durante il salvataggio: {ex.Message}");
            }
        }


        /// <summary>
        /// Funzione per chiudere tutte le finestre dei Task aperte
        /// </summary>
        private void CloseAllTaskCanvasWindows()
        {
            //MessageBox.Show(_taskCanvasWindows.Count.ToString(), "Numero di finestre aperte che verranno chiuse:");

            foreach (var window in _taskCanvasWindows)
            {
                if (window != null)
                {
                    window.Close();
                }
            }
            _taskCanvasWindows.Clear();  // Clear the list after closing all the windows
        }

        ///// <summary>
        ///// Check se il file del Task esiste 
        ///// </summary>
        ///// <param name="filePath"></param>
        ///// <returns></returns>
        //public bool CsvFileExists(string filePath)
        //{
        //    // Check if the file has a .csv extension and if it exists
        //    return Path.GetExtension(filePath).ToLower() == ".csv" && File.Exists(filePath);
        //}


        /// <summary>
        /// Check se il file del Task esiste con verifica robusta
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Tuple with existence status and file size</returns>
        public (bool exists, long fileSize) CsvFileExists(string filePath)
        {
            try
            {
                if (!Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, 0);
                }

                if (!File.Exists(filePath))
                {
                    return (false, 0);
                }

                // Get file info for additional verification
                var fileInfo = new FileInfo(filePath);
                return (true, fileInfo.Length);
            }
            catch (Exception)
            {
                return (false, 0);
            }
        }


        /// <summary>
        /// Funzione collegata al pulsante Save della View
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void RealTimeInk_PenData_Save(object sender, RoutedEventArgs e)
        //{

        //    if (_realTimeInk_PenData.Count > 0)
        //    {
        //        var result = CustomMessageBox.ShowOKCancel("Il Task è stato eseguito correttamente?",
        //                                                   "Esecuzione Terminata",
        //                                                   "Si, Salva Task",
        //                                                   "No, Ripeti il Task");

        //        RealTimeInk_StartStop = false;
        //        NewTaskStartButtonEnabled = true;
        //        SaveTaskButtonEnabled = false;
        //        SkipTaskButtonEnabled = true;

        //        if (result == MessageBoxResult.OK)
        //        {

        //            //string taskFilename = Path.GetFileNameWithoutExtension(PathCurrentTask) + "_" + PatientId + ".csv";

        //            string taskFilename = "Task" + _trueTaskIndex.ToString() + "_" + PatientId + ".csv";

        //            // Path completo del file csv dello specifico Task
        //            string taskFilePath = Path.Combine(DataPath, taskFilename);

        //            // _taskCanvasWindow.Close();
        //            CloseAllTaskCanvasWindows();


        //            if (!SavePenData(taskFilePath))
        //            {
        //                MessageBox.Show("Errore durante il salvataggio");
        //            }

        //            if (CsvFileExists(taskFilePath))
        //            {
        //                MessageBox.Show("File salvato correttamente");
        //            }
        //            else
        //            {
        //                MessageBox.Show("Ripetere il Task");
        //            }

        //            //Task.Run(() => RealTimeInk_SavePenData(taskFilePath), _cancellationToken.Token);

        //            // Effettua il salvataggio in modo asincrono
        //            //Task.Run(async () => await RealTimeInk_SavePenData(taskFilePath), _cancellationToken.Token);

        //            // Reinizializza il Counter dei punti in memoria
        //            _realTimeInk_PenData_Last = null;
        //            _realTimeInk_PenData.Clear();

        //            // Aumenta contatore e passo al task successivo
        //            _taskCounter += 1;

        //            // Aumenta contatore e passo al task successivo
        //            _trueTaskIndex += 1;

        //            // Count the csv files in the folder
        //            _numberCsvFile = Directory.GetFiles(DataPath, "*.csv").Length;

        //            if (_taskCounter >= _totalNumberOfTask)
        //            {
        //                MessageBox.Show("Ultimo Task eseguito, Finestra in chiusura...");
        //                this.Close();
        //            }
        //            else
        //            {
        //                PathCurrentTask = TaskImageList[_taskCounter];

        //                if (!_isInstructionFolderEmpty)
        //                {
        //                    PathTextCurrentTask = TaskTextList[_taskCounter];

        //                    //MessageBox.Show(File.ReadAllText(PathTextCurrentTask), "Testo txt ");

        //                    InstructionBoxText = "";
        //                    InstructionBoxText = File.ReadAllText(PathTextCurrentTask);
        //                }

        //                // Aggiornamento testo nella TextBox
        //                //PathTextCurrentTask = TaskTextList[_taskCounter];

        //                //InfoTextBox.Text = "";

        //                //InfoTextBox.Text = File.ReadAllText(PathTextCurrentTask.FullName);

        //                // Aggiorna la UI
        //                OnPenDataPropertyChanged();
        //            }
        //        }
        //        else
        //        {
        //            // Reinizializza il Counter dei punti in memoria
        //            _realTimeInk_PenData_Last = null;
        //            _realTimeInk_PenData.Clear();

        //            // Aggiorna la UI
        //            OnPenDataPropertyChanged();

        //            // Chiudo la finestra del Task 
        //            CloseAllTaskCanvasWindows();
        //        }

        //    }
        //    else
        //    {
        //        MessageBox.Show("Nessun Punto Acquisito, Task da ripetere! Premi di nuovo Somministra");
        //        RealTimeInk_StartStop = false;
        //        NewTaskStartButtonEnabled = true;
        //        SaveTaskButtonEnabled = false;
        //        SkipTaskButtonEnabled = true;
        //        // Reinizializza il Counter dei punti in memoria
        //        _realTimeInk_PenData_Last = null;
        //        _realTimeInk_PenData.Clear();

        //        // Aggiorna la UI
        //        OnPenDataPropertyChanged();

        //        // Chiudo la finestra del Task 
        //        CloseAllTaskCanvasWindows();
        //    }

        //    GC.Collect();
        //}

        /// <summary>
        /// Funzione collegata al pulsante Save della View con miglioramenti di sicurezza e verifica
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RealTimeInk_PenData_Save(object sender, RoutedEventArgs e)
        {
            int dataCount;

            // Thread-safe check for data
            lock (_penDataLock)
            {
                dataCount = _realTimeInk_PenData.Count;
            }

            if (dataCount > 0)
            {
                var result = CustomMessageBox.ShowOKCancel("Il Task è stato eseguito correttamente?",
                                                           "Esecuzione Terminata",
                                                           "Si, Salva Task",
                                                           "No, Ripeti il Task");

                // Stop ink capture immediately
                RealTimeInk_StartStop = false;

                // Stop backup service
                _backupService.StopBackup();

                NewTaskStartButtonEnabled = true;
                SaveTaskButtonEnabled = false;
                SkipTaskButtonEnabled = true;

                if (result == MessageBoxResult.OK)
                {
                    // Extract task name from current image path for the filename
                    string taskName = Path.GetFileNameWithoutExtension(PathCurrentTask);
                    string taskFilename = $"{taskName}_{PatientId}.csv";
                    string taskFilePath = Path.Combine(DataPath, taskFilename);

                    // Close all task windows first
                    CloseAllTaskCanvasWindows();

                    // Update status
                    DeviceStatus = "Salvando i dati...";

                    // Create a background task for saving to keep UI responsive
                    Task.Run(() =>
                    {
                        var (success, pointsSaved, message) = SavePenData(taskFilePath);

                        // Update UI on the UI thread
                        _synchronizationContext.Post(o =>
                        {
                            if (success)
                            {
                                // Clean up backup files since save was successful
                                _backupService.CleanupBackup();

                                MessageBox.Show(message, "Salvataggio completato", MessageBoxButton.OK, MessageBoxImage.Information);

                                // Clear pen data after successful save
                                lock (_penDataLock)
                                {
                                    _realTimeInk_PenData_Last = null;
                                    _realTimeInk_PenData.Clear();
                                }

                                // Increment task counter
                                _taskCounter += 1;

                                // Update file count for display
                                _numberCsvFile = Directory.GetFiles(DataPath, "*.csv").Length;

                                // Check if we've completed all tasks
                                if (_taskCounter >= _totalNumberOfTask)
                                {
                                    MessageBox.Show("Ultimo Task eseguito, Finestra in chiusura...");
                                    this.Close();
                                }
                                else
                                {
                                    // Set up the next task
                                    PathCurrentTask = TaskImageList[_taskCounter];

                                    // Update instructions if available
                                    if (!_isInstructionFolderEmpty && _taskCounter < TaskTextList.Count)
                                    {
                                        PathTextCurrentTask = TaskTextList[_taskCounter];

                                        try
                                        {
                                            InstructionBoxText = File.ReadAllText(PathTextCurrentTask);
                                        }
                                        catch (Exception ex)
                                        {
                                            InstructionBoxText = $"Errore lettura istruzioni: {ex.Message}";
                                        }
                                    }

                                    // Refresh UI
                                    OnPenDataPropertyChanged();
                                }
                            }
                            else
                            {
                                // Check if backup is available
                                string backupPath = _backupService.GetBackupFile();
                                if (backupPath != null)
                                {
                                    var recoverResult = MessageBox.Show(
                                        $"Il salvataggio è fallito con errore: {message}\n\nÈ disponibile un backup. Vuoi recuperarlo?",
                                        "Errore salvataggio",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Warning);

                                    if (recoverResult == MessageBoxResult.Yes)
                                    {
                                        try
                                        {
                                            // Copy backup to actual task file
                                            File.Copy(backupPath, taskFilePath, true);
                                            MessageBox.Show("Backup ripristinato con successo.", "Ripristino completato", MessageBoxButton.OK, MessageBoxImage.Information);

                                            // Clean up backup files since recovery was successful
                                            _backupService.CleanupBackup();

                                            // Continue with task progression
                                            lock (_penDataLock)
                                            {
                                                _realTimeInk_PenData_Last = null;
                                                _realTimeInk_PenData.Clear();
                                            }

                                            _taskCounter += 1;
                                            _numberCsvFile = Directory.GetFiles(DataPath, "*.csv").Length;

                                            if (_taskCounter >= _totalNumberOfTask)
                                            {
                                                MessageBox.Show("Ultimo Task eseguito, Finestra in chiusura...");
                                                this.Close();
                                            }
                                            else
                                            {
                                                PathCurrentTask = TaskImageList[_taskCounter];
                                                if (!_isInstructionFolderEmpty && _taskCounter < TaskTextList.Count)
                                                {
                                                    PathTextCurrentTask = TaskTextList[_taskCounter];
                                                    try
                                                    {
                                                        InstructionBoxText = File.ReadAllText(PathTextCurrentTask);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        InstructionBoxText = $"Errore lettura istruzioni: {ex.Message}";
                                                    }
                                                }
                                                OnPenDataPropertyChanged();
                                            }
                                            return;
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show($"Errore durante il ripristino del backup: {ex.Message}",
                                                "Errore ripristino",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Error);
                                        }
                                    }
                                }

                                // Show error message if save failed and backup recovery wasn't successful
                                MessageBox.Show(message, "Errore salvataggio", MessageBoxButton.OK, MessageBoxImage.Error);
                            }

                            // Restore device status
                            DeviceStatus = "Connected";

                        }, null);
                    });
                }
                else
                {
                    // User chose to repeat the task
                    lock (_penDataLock)
                    {
                        _realTimeInk_PenData_Last = null;
                        _realTimeInk_PenData.Clear();
                    }

                    // Update UI
                    OnPenDataPropertyChanged();

                    // Close task windows
                    CloseAllTaskCanvasWindows();
                }
            }
            else
            {
                MessageBox.Show("Nessun Punto Acquisito, Task da ripetere! Premi di nuovo Somministra",
                                "Avviso", MessageBoxButton.OK, MessageBoxImage.Warning);

                RealTimeInk_StartStop = false;
                // Stop backup service
                _backupService.StopBackup();

                NewTaskStartButtonEnabled = true;
                SaveTaskButtonEnabled = false;
                SkipTaskButtonEnabled = true;

                // Clear data
                lock (_penDataLock)
                {
                    _realTimeInk_PenData_Last = null;
                    _realTimeInk_PenData.Clear();
                }

                // Update UI
                OnPenDataPropertyChanged();

                // Close task windows
                CloseAllTaskCanvasWindows();
            }

            // Suggest garbage collection to clean up resources
            GC.Collect();
        }

        ///// <summary>
        ///// Funzione per poter passare al Task successivo senza dover salvare 
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void SkipToNextTask(object sender, RoutedEventArgs e)
        //{

        //    NewTaskStartButtonEnabled = true;

        //    RealTimeInk_StartStop = false;

        //    var result = CustomMessageBox.ShowOKCancel("Passare al Task Successivo?",
        //                                                   "Esecuzione Terminata",
        //                                                   "Si",
        //                                                   "No");

        //    if (result == MessageBoxResult.OK)
        //    {

        //        // Reinizializza il Counter dei punti in memoria
        //        _realTimeInk_PenData_Last = null;
        //        _realTimeInk_PenData.Clear();

        //        // Aumenta contatore e passo al task successivo
        //        _taskCounter += 1;
        //        _trueTaskIndex += 1;

        //        if (_taskCounter >= _totalNumberOfTask)
        //        {
        //            MessageBox.Show("Ultimo Task eseguito, Finestra in chiusura...");
        //            this.Close();
        //        }
        //        else
        //        {
        //            PathCurrentTask = TaskImageList[_taskCounter];

        //            if (!_isInstructionFolderEmpty)
        //            {
        //                PathTextCurrentTask = TaskTextList[_taskCounter];

        //                //MessageBox.Show(File.ReadAllText(PathTextCurrentTask), "Testo txt ");

        //                InstructionBoxText = "";
        //                InstructionBoxText = File.ReadAllText(PathTextCurrentTask);
        //            }
        //        }

        //        // Aggiorna la UI
        //        OnPenDataPropertyChanged();

        //        CloseAllTaskCanvasWindows();

        //    }
        //}


        /// <summary>
        /// Funzione thread-safe per poter passare al Task successivo senza dover salvare
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkipToNextTask(object sender, RoutedEventArgs e)
        {
            NewTaskStartButtonEnabled = true;
            RealTimeInk_StartStop = false;

            // Stop backup service
            _backupService.StopBackup();

            var result = CustomMessageBox.ShowOKCancel("Passare al Task Successivo?",
                                                       "Esecuzione Terminata",
                                                       "Si",
                                                       "No");

            if (result == MessageBoxResult.OK)
            {
                // Thread-safe clearing of pen data
                lock (_penDataLock)
                {
                    _realTimeInk_PenData_Last = null;
                    _realTimeInk_PenData.Clear();
                }

                // Clean up any backups for the current task since we're skipping it
                _backupService.CleanupBackup();

                // Increment task counter
                _taskCounter += 1;

                if (_taskCounter >= _totalNumberOfTask)
                {
                    MessageBox.Show("Ultimo Task eseguito, Finestra in chiusura...");
                    this.Close();
                }
                else
                {
                    // Update to the next task
                    PathCurrentTask = TaskImageList[_taskCounter];

                    // Update instructions if available
                    if (!_isInstructionFolderEmpty && _taskCounter < TaskTextList.Count)
                    {
                        PathTextCurrentTask = TaskTextList[_taskCounter];

                        try
                        {
                            InstructionBoxText = File.ReadAllText(PathTextCurrentTask);
                        }
                        catch (Exception ex)
                        {
                            InstructionBoxText = $"Errore lettura istruzioni: {ex.Message}";
                        }
                    }
                }

                // Update UI
                OnPenDataPropertyChanged();

                // Close all task windows
                CloseAllTaskCanvasWindows();
            }
        }


        ///// <summary>
        ///// Funzione per poter tornare al Task precedente senza dover salvare
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void GoPreviousTask(object sender, RoutedEventArgs e)
        //{

        //    NewTaskStartButtonEnabled = true;

        //    RealTimeInk_StartStop = false;

        //    var result = CustomMessageBox.ShowOKCancel("Tornare al Task precedente?",
        //                                                   "Esecuzione Terminata",
        //                                                   "Si",
        //                                                   "No");

        //    if (result == MessageBoxResult.OK)
        //    {
        //        if (_taskCounter == 0 || _trueTaskIndex == 0)
        //        {
        //            MessageBox.Show("Primo Task, impossibile tornare indietro!");
        //        }
        //        else
        //        {
        //            MessageBox.Show("Il task precedentemente acquisito verrà sovrascritto!");

        //            // Reinizializza il Counter dei punti in memoria
        //            _realTimeInk_PenData_Last = null;
        //            _realTimeInk_PenData.Clear();

        //            // Aumenta contatore e passo al task successivo
        //            _taskCounter -= 1;
        //            _trueTaskIndex -= 1;

        //            PathCurrentTask = TaskImageList[_taskCounter];

        //            if (!_isInstructionFolderEmpty)
        //            {
        //                PathTextCurrentTask = TaskTextList[_taskCounter];

        //                //MessageBox.Show(File.ReadAllText(PathTextCurrentTask), "Testo txt ");

        //                InstructionBoxText = "";
        //                InstructionBoxText = File.ReadAllText(PathTextCurrentTask);
        //            }

        //            // Aggiorna la UI
        //            OnPenDataPropertyChanged();

        //            CloseAllTaskCanvasWindows();
        //        }

        //    }
        //}


        /// <summary>
        /// Funzione thread-safe per poter tornare al Task precedente senza dover salvare
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GoPreviousTask(object sender, RoutedEventArgs e)
        {
            NewTaskStartButtonEnabled = true;
            RealTimeInk_StartStop = false;

            // Stop backup service
            _backupService.StopBackup();

            var result = CustomMessageBox.ShowOKCancel("Tornare al Task precedente?",
                                                       "Esecuzione Terminata",
                                                       "Si",
                                                       "No");

            if (result == MessageBoxResult.OK)
            {
                if (_taskCounter == 0)
                {
                    MessageBox.Show("Primo Task, impossibile tornare indietro!");
                }
                else
                {
                    MessageBox.Show("Il task precedentemente acquisito verrà sovrascritto!");

                    // Clean up any backups for the current task
                    _backupService.CleanupBackup();

                    // Thread-safe clearing of pen data
                    lock (_penDataLock)
                    {
                        _realTimeInk_PenData_Last = null;
                        _realTimeInk_PenData.Clear();
                    }

                    // Decrement task counter
                    _taskCounter -= 1;

                    // Update to the previous task
                    PathCurrentTask = TaskImageList[_taskCounter];

                    // Update instructions if available
                    if (!_isInstructionFolderEmpty && _taskCounter < TaskTextList.Count)
                    {
                        PathTextCurrentTask = TaskTextList[_taskCounter];

                        try
                        {
                            InstructionBoxText = File.ReadAllText(PathTextCurrentTask);
                        }
                        catch (Exception ex)
                        {
                            InstructionBoxText = $"Errore lettura istruzioni: {ex.Message}";
                        }
                    }

                    // Update UI
                    OnPenDataPropertyChanged();

                    // Close all task windows
                    CloseAllTaskCanvasWindows();
                }
            }
        }


        private string? ValueToString<T>(T? value) where T : struct
        {
            return value != null && value.HasValue ? value.ToString() : "";
        }

        private string ValueToHexString(int? value)
        {
            return value != null && value.HasValue ? $"0x{value.Value:x8}" : "";
        }

        public string? RealTimeInk_Timestamp => _realTimeInk_PenData_Last?.Timestamp.ToString("O");
        public string? RealTimeInk_Point => _realTimeInk_PenData_Last?.Point.ToString();
        public string? RealTimeInk_Phase => _realTimeInk_PenData_Last?.Phase.ToString();
        public string RealTimeInk_Pressure => ValueToString(_realTimeInk_PenData_Last?.Pressure);
        public string RealTimeInk_PointDisplay => ValueToString(_realTimeInk_PenData_Last?.PointDisplay);
        public string RealTimeInk_PointRaw => ValueToString(_realTimeInk_PenData_Last?.PointRaw);
        public string RealTimeInk_PressureRaw => ValueToString(_realTimeInk_PenData_Last?.PressureRaw);
        public string RealTimeInk_TimestampRaw => ValueToString(_realTimeInk_PenData_Last?.TimestampRaw);
        public string RealTimeInk_Sequence => ValueToString(_realTimeInk_PenData_Last?.Sequence);
        public string RealTimeInk_Rotation => ValueToString(_realTimeInk_PenData_Last?.Rotation);
        public string RealTimeInk_Azimuth => ValueToString(_realTimeInk_PenData_Last?.Azimuth);
        public string RealTimeInk_Altitude => ValueToString(_realTimeInk_PenData_Last?.Altitude);
        public string RealTimeInk_Tilt => ValueToString(_realTimeInk_PenData_Last?.Tilt);
        public string RealTimeInk_PenId => ValueToHexString(_realTimeInk_PenData_Last?.PenId);
        #endregion

        /// <summary>
        /// Chiude tutte le finestre figlie aperte e cancella i file nella directory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Attenzione! Finestra in chiusura!",
                "Conferma", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Stop backup service
                _backupService.StopBackup();

                GC.Collect();
                CloseAllTaskCanvasWindows();  // Close all child windows
            }
            else
            {
                e.Cancel = true;  // Cancel the close operation
            }
        }


    }




}