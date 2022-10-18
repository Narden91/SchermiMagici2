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
		private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
		private readonly ExperimentStore _experimentStore;
		private TaskCanvas _taskCanvasWindow;
        private List<string> _taskToNotRecordList = new List<string> { "Task_2", "Task_3", "Task_6", "Task_26" };

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

				//MessageBox.Show(File.ReadAllText(PathTextCurrentTask), "Testo txt ");

				InstructionBoxText = "";
				InstructionBoxText = File.ReadAllText(PathTextCurrentTask);

			}
			else
				InstructionBoxText = "Nessun file di Istruzioni trovato.";

            _imageSavingFolderPath = Path.Combine(DataPath, "Images");

            // Crea la Cartella in Documenti/Application_saving_folder/..
            Directory.CreateDirectory(_imageSavingFolderPath);


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


		/// <summary>
		/// Funzione collegata al pulsante Open nella View
		/// Apre la finestra del task i-esimo in un nuovo thread
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OpenTaskWindow(object sender, RoutedEventArgs e)
		{
			// Gestione abilitazione Pulsanti alla somministrazione del Task-i
            RealTimeInk_StartStop = true;
            MoveTaskButtonEnabled = true;
            NewTaskStartButtonEnabled = false;

			// Path e nome dell'immagine da salvare per il Task-i
            string imageTaskPath = Path.Combine(_imageSavingFolderPath, TaskNameToShowToUI + ".png");

			// Crea nuova finestra InkCanvas in un nuovo Thread
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
			{
				_taskCanvasWindow = new TaskCanvas(PathCurrentTask, imageTaskPath);
				_taskCanvasWindow.Show();
			}));

		}

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

        #region Gestione Pulsanti e funzioni collegate ai Pulsanti 

        #region Pulsante Successivo 

        private bool _moveTaskButtonEnabled = false;

        /// <summary>
        /// Funzione Per gestire l'abilitazione del pulsante Salva
        /// </summary> 
        public bool MoveTaskButtonEnabled
        {
            get => _moveTaskButtonEnabled;
            set
            {
                _moveTaskButtonEnabled = value;
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

        #region Attributi privati gestione punti acquisiti
        private List<Wacom.Devices.RealTimePointReceivedEventArgs> _realTimeInk_PenData = new List<Wacom.Devices.RealTimePointReceivedEventArgs>();
		private Wacom.Devices.RealTimePointReceivedEventArgs _realTimeInk_PenData_Last;
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
        public string RealTimeInk_IsStarted => (_realTimeInkService?.IsStarted ?? false) ? "SI" : "NO";

		// Funzione per effettuare il refresh della UI 
		private void OnPenDataPropertyChanged()
		{
			_synchronizationContext.Post(OnPenDataPropertyChangedSync, null);

			void OnPenDataPropertyChangedSync(object _) // This must be called on the UI thread.
			{
				var last = _realTimeInk_PenData_Last;

				if (PropertyChanged != null)
				{
					PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(MoveTaskButtonEnabled)));
					//PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(SkipTaskButtonEnabled)));
					PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(RealTimeInk_Count_int)));
					PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(NewTaskStartButtonEnabled)));
					PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(PathCurrentTask)));
					PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(TaskNameToShowToUI)));
					PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(InstructionBoxText)));
				}
			}
		}

		/// <summary>
		/// Gestione punti ricevuti dal dispositivo in tempo reale
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void _realTimeInkService_PointReceived(object sender, Wacom.Devices.RealTimePointReceivedEventArgs e)
		{
			_realTimeInk_PenData_Last = e;
			_realTimeInk_PenData.Add(_realTimeInk_PenData_Last);
			OnPenDataPropertyChanged();
		}

		/// <summary>
		/// Funzione per mostrare sulla UI il numero di punti acquisiti
		/// </summary>
		public int RealTimeInk_Count_int => _realTimeInk_PenData.Count;

		/// <summary>
		/// Funzione per effettuare il clear del buffer di punti ricevuti dal dispositivo
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void RealTimeInk_PenData_Clear(object sender, RoutedEventArgs e)
		{
			_realTimeInk_PenData_Last = null;
			_realTimeInk_PenData.Clear();
			OnPenDataPropertyChanged();
		}

		/// <summary>
		/// Esegue il salvataggio dei dati rilevati dalla penna del device utilizzato
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		private bool SavePenData(string fileName)
		{
			bool status = false;
			try
			{
				using var stream = File.CreateText(fileName);
				stream.WriteLine("Timestamp,PointX,PointY,Phase,Pressure,PointDisplayX,PointDisplayY,PointRawX,PointRawY,PressureRaw,TimestampRaw,Sequence,Rotation,Azimuth,Altitude,TiltX,TiltY,PenId");

				StringBuilder sb = new StringBuilder();
				foreach (var item in _realTimeInk_PenData)
				{
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
					sb.Clear();
					status = true;
				}
				stream.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Unable to load image: {ex.Message}");
			}
			return status;
		}

       
        /// <summary>
        /// Funzione collegata al pulsante SUCCESSIVO della View
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RealTimeInk_PenData_Save(object sender, RoutedEventArgs e)
        {

			if (_taskToNotRecordList.Contains(TaskNameToShowToUI))
			{
                //Task da Saltare
                var result = CustomMessageBox.ShowOKCancel("Passare al Task Successivo?",
                                                           "Esecuzione Terminata",
                                                           "Si",
                                                           "No");
                if (result == MessageBoxResult.OK)
                {

                    // Reinizializza il Counter dei punti in memoria
                    _realTimeInk_PenData_Last = null;
                    _realTimeInk_PenData.Clear();

                    // Aumenta contatore e passo al task successivo
                    _taskCounter += 1;

                    if (_taskCounter >= _totalNumberOfTask)
                    {
                        MessageBox.Show("Ultimo Task eseguito, Finestra in chiusura...");
                        this.Close();
                    }
                    else
                    {
                        PathCurrentTask = TaskImageList[_taskCounter];

                        if (!_isInstructionFolderEmpty)
                        {
                            PathTextCurrentTask = TaskTextList[_taskCounter];

                            InstructionBoxText = "";
                            InstructionBoxText = File.ReadAllText(PathTextCurrentTask);
                        }
                    }

                    // Aggiorna la UI
                    OnPenDataPropertyChanged();

					// Gestione Abilitazione Pulsanti
					MoveTaskButtonEnabled = false;
                   
                    RealTimeInk_StartStop = false;

                    // Riabilito il pulsante di somministrazione Task
                    NewTaskStartButtonEnabled = true;

                    // Chiudo la finestra del Task 
                    _taskCanvasWindow.Close();
                }

            }
			else
			{
                if (_realTimeInk_PenData.Count > 0)
                {
                    var result = CustomMessageBox.ShowOKCancel("Il Task è stato eseguito correttamente?",
                                                               "Esecuzione Terminata",
                                                               "Si, Salva Task",
                                                               "No, Ripeti");

                    if (result == MessageBoxResult.OK)
                    {
						// Filename per il .csv da generare
                        string taskFilename = "Task" + _trueTaskIndex.ToString() + "_" + PatientId + ".csv";

                        // Path completo del file csv dello specifico Task
                        string taskFilePath = Path.Combine(DataPath, taskFilename);

						// Chiudo finestra InkCanvas
                        _taskCanvasWindow.Close();

                        // Elimina memoria inutilizzata
                        GC.Collect();

                        if (!SavePenData(taskFilePath))
                        {
                            MessageBox.Show("Errore durante il salvataggio");
                        }

                        // Reinizializza il Counter dei punti in memoria
                        _realTimeInk_PenData_Last = null;
                        _realTimeInk_PenData.Clear();

                        // Aumenta contatore e passo al task successivo
                        _taskCounter += 1;

                        // Aumenta contatore e passo al task successivo
                        _trueTaskIndex += 1;

                        if (_taskCounter >= _totalNumberOfTask)
                        {
                            MessageBox.Show("Ultimo Task eseguito, Finestra in chiusura...");
                            this.Close();
                        }
                        else
                        {
                            PathCurrentTask = TaskImageList[_taskCounter];

                            if (!_isInstructionFolderEmpty)
                            {
                                PathTextCurrentTask = TaskTextList[_taskCounter];

                                InstructionBoxText = "";
                                InstructionBoxText = File.ReadAllText(PathTextCurrentTask);
                            }

                            // Aggiorna la UI
                            OnPenDataPropertyChanged();

                            // Gestione Abilitazione Pulsanti
                            MoveTaskButtonEnabled = false;

                            RealTimeInk_StartStop = false;

                            // Riabilito il pulsante di somministrazione Task
                            NewTaskStartButtonEnabled = true;
                        }
                    }
                    else
                    {
                        // Reinizializza il Counter dei punti in memoria
                        _realTimeInk_PenData_Last = null;
                        _realTimeInk_PenData.Clear();

                        // Aggiorna la UI
                        OnPenDataPropertyChanged();

                        // Gestione Abilitazione Pulsanti
                        MoveTaskButtonEnabled = false;

                        RealTimeInk_StartStop = false;

                        // Riabilito il pulsante di somministrazione Task
                        NewTaskStartButtonEnabled = true;

                        // Chiudo la finestra del Task 
                        _taskCanvasWindow.Close();
                    }

                }
            }

        }

        #endregion


        #region Funzioni Accessorie Wacom
        private string ValueToString<T>(T? value) where T : struct
        {
            return value != null && value.HasValue ? value.ToString() : "";
        }

        private string ValueToHexString(int? value)
        {
            return value != null && value.HasValue ? $"0x{value.Value:x8}" : "";
        }

        public string RealTimeInk_Timestamp => _realTimeInk_PenData_Last?.Timestamp.ToString("O");
        public string RealTimeInk_Point => _realTimeInk_PenData_Last?.Point.ToString();
        public string RealTimeInk_Phase => _realTimeInk_PenData_Last?.Phase.ToString();
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

        private void OnWindowclose(object sender, EventArgs e)
		{
			Environment.Exit(Environment.ExitCode); // Prevent memory leak
			Application.Current.Shutdown();
		}

	}

}
