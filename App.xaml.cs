using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfApp1.Services;
using WpfApp1.Stores;
using WpfApp1.ViewModels;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Campi condivisi
        private readonly NavigationStore _navigationStore;
        private PatientStore _patientStore;
        private ProctorStore _proctorStore;
        private ExperimentStore _experimentStore;
        private DeviceConnectionStore _deviceConnectionStore;
        private static Mutex _mutex = null; 
        #endregion


        public App()
        {
            // Inizializza nuovi oggetti STORE allo startup dell'applicazione
            // e le cui informazioni verranno condivise tra le varie page 
            _navigationStore = new NavigationStore();
            _patientStore = new PatientStore();
            _proctorStore = new ProctorStore();
            _experimentStore = new ExperimentStore();
            _deviceConnectionStore = new DeviceConnectionStore();

            // Effettua il controllo sulla validità della licenza Wacom inserita nel
            // file app.config
            InitializeWacomLicense();
        }

        /// <summary>
        /// This method is responsible of the MainWindow startup and the Datacontext managing
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Check for multiple instances using Mutex
            const string appName = "SchermiMagici";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("The app is already running!");
                Shutdown(); // Shutdown the current application
                return;
            }

            _navigationStore.CurrentViewModel = CreateHomeViewModel();

            MainWindow = new MainWindow()
            {
                DataContext = new MainViewModel(_navigationStore)
            };

            MainWindow.Show();

            base.OnStartup(e);
        }

        #region Directory and path Functions
        /// <summary>
        /// Funzione per creare il Path della cartella Radice degli esperimenti =>
        /// Documenti/WacomApp/
        /// </summary>
        /// <returns></returns>
        internal static string GetAppFolder()
        {
            // Prelevo il path della cartella Documenti
            string savingFolder = (string)Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Aggiungo la sottocartella ApplicationSavingFolder, nella quale verranno salvati i singoli pazienti
            savingFolder = Path.Combine(savingFolder, "Application_Saving_Folder");

            return savingFolder;
        }
        #endregion

        #region AppId (used by BLE devices for pairing)
        private class MyAppId : Wacom.Devices.IApplicationIdentifier
        {
            public byte[] ToByteArray() => new byte[] { 0xFA, 0xAB, 0xC1, 0xE0, 0xF1, 0x77 };
        };

        static internal Wacom.Devices.IApplicationIdentifier AppId => new MyAppId();
        #endregion

        #region Transport Images
        private static readonly ImageSource[] _images = new[]
        {
            null, // unknown
			new BitmapImage(new Uri(@"pack://application:,,,/Resources/ble.png")),
            null, // btc
			new BitmapImage(new Uri(@"pack://application:,,,/Resources/usb.png")),
            new BitmapImage(new Uri(@"pack://application:,,,/Resources/hid.png")),
            null, // serial
			new BitmapImage(new Uri(@"pack://application:,,,/Resources/wac.png"))
        };

        internal static ImageSource TransportImage(Wacom.Devices.TransportProtocol transportProtocol)
        {

            return _images[(int)transportProtocol];
        }
        #endregion

        #region DiscreteDisplayService Sample Image
        internal static System.Drawing.Bitmap DiscreteDisplaySampleImage()
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(new BitmapImage(new Uri("pack://application:,,,/Resources/DiscreteDisplaySampleImage.png"))));
            using var stream = new System.IO.MemoryStream();
            encoder.Save(stream);
            stream.Flush();
            return new System.Drawing.Bitmap(stream);
        }
        #endregion

        #region Initial Wacom Licensing
        private static void InitializeWacomLicense()
        {
            var license = ConfigurationManager.AppSettings["Wacom.License"];
            if (license == null || license.Length == 0)
                license = Environment.GetEnvironmentVariable("Wacom.License");

            if (license != null && license.Length > 0)
            {
                Wacom.Licensing.LicenseValidator.Instance.SetLicense(license);
                if (DateTime.Now > Wacom.Licensing.LicenseValidator.Instance.ExpiryDate())
                {
                    MessageBox.Show("The license has expired, please obtain a new one");
                }
                //else
                //{
                //    MessageBox.Show("The License is Valid");
                //}
            }
            else
            {
                MessageBox.Show("No license found. Please add to app.config or as an environment variable called \"Wacom.License\".");
            }

        }
        #endregion

        #region Functions startup for ViewModels

        /// <summary>
        /// 1.Function to display the Home View
        /// </summary>
        /// <returns></returns>
        private HomeViewModel CreateHomeViewModel()
        {
            return new HomeViewModel(new NavigationService(_navigationStore, CreateProctorPageViewModel));
        }

        /// <summary>
        /// 2.Function to display the Proctor Page View 
        /// </summary>
        /// <returns></returns>
        private ProctorPersonalInfoPageViewModel CreateProctorPageViewModel()
        {
            return new ProctorPersonalInfoPageViewModel(new NavigationService(_navigationStore, CreatePatientInfoViewModel), _proctorStore);
        }


        /// <summary>
        /// 3.Function to display the Patient info View
        /// </summary>
        /// <returns></returns>
        private PatientInfoInputPageViewModel CreatePatientInfoViewModel()
        {
            return new PatientInfoInputPageViewModel(new NavigationService(_navigationStore, CreateDevicePageViewModel), _patientStore, _proctorStore);
        }

        /// <summary>
        /// 4.Function to display the Device Page View 
        /// </summary>
        /// <returns></returns>
        private DevicePageViewModel CreateDevicePageViewModel()
        {
            return new DevicePageViewModel(_patientStore, _experimentStore, _deviceConnectionStore, new NavigationService(_navigationStore, CreatePatientInfoViewModel));
        }
        #endregion
    }
}
