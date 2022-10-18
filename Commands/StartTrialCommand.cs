using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using WpfApp1.Models;
using WpfApp1.Stores;
using WpfApp1.ViewModels;

namespace WpfApp1.Commands
{
    public class StartTrialCommand : CommandBase
    {

        //private readonly NavigationService _navigationService;
        private readonly DevicePageViewModel _devicePageViewModel;
        //private readonly PatientStore _patientStore;
        private readonly ExperimentStore _experimentStore;
        private readonly DeviceConnectionStore _deviceConnectionStore;

        private DeviceWindow _deviceWindow;

        #region Costruttore inutilizzato
        //public StartTrialCommand(DevicePageViewModel devicePageViewModel, NavigationService navigationService, 
        //                         ProctorStore proctorStore, PatientStore patientStore, ExperimentStore experimentStore)
        //{
        //    _devicePageViewModel = devicePageViewModel;
        //    _navigationService = navigationService;
        //    _proctorStore = proctorStore;
        //    _patientStore = patientStore;   
        //}

        //public StartTrialCommand(DevicePageViewModel devicePageViewModel, NavigationService navigationService, 
        //                         ExperimentStore experimentStore, DeviceConnectionStore deviceConnectionStore)
        //{
        //    _devicePageViewModel = devicePageViewModel;
        //    _navigationService = navigationService;
        //    _experimentStore = experimentStore;
        //    _deviceConnectionStore = deviceConnectionStore;
        //}
        #endregion

        public StartTrialCommand(DevicePageViewModel devicePageViewModel,
                                ExperimentStore experimentStore, DeviceConnectionStore deviceConnectionStore)
        {
            _devicePageViewModel = devicePageViewModel;
            _experimentStore = experimentStore;
            _deviceConnectionStore = deviceConnectionStore;
        }


        /// <summary>
        /// Funzione che esegue operazioni alla pressione del pulsante 
        /// StartTrialCommand nella View della connessione dispositivi
        /// </summary>
        /// <param name="parameter"></param>
        public override void Execute(object parameter)
        {
            
            if (_devicePageViewModel.SelectedDevice == null)
            {
                MessageBox.Show("Nessun Dispositivo Selezionato!");

                #region Debug 1
                //string savingFolder = App.GetAppFolder();

                //savingFolder = Path.Combine(savingFolder, _devicePageViewModel.PatientExperimentOutputFolder);

                //MessageBox.Show(savingFolder, "Directory:");

                ////System.IO.Directory.CreateDirectory(savingFolder);

                //bool IsFolderCreated = File.Exists(savingFolder);

                //if (IsFolderCreated)
                //{
                //    MessageBox.Show("Directory esistente");

                //    //System.IO.Directory.CreateDirectory(savingFolder);
                //    // Debug 
                //    //MessageBox.Show(savingFolder, "Directory creata");
                //}
                //else
                //{
                //    MessageBox.Show("Directory non presente");
                //    Directory.CreateDirectory(savingFolder);
                //    MessageBox.Show("Directory creata!");
                //    // Debug 
                //    //MessageBox.Show(savingFolder, "Directory già esistente");
                //}
                //// Debug 
                ////MessageBox.Show(savingFolder, "Directory");
                #endregion

            }
            else
            {
                #region Debug 2
                //MessageBox.Show(_devicePageViewModel.SelectedDevice.ToString(), "Device selezionato: ");

                //string savingFolder = App.GetAppFolder();

                //savingFolder = Path.Combine(savingFolder, _devicePageViewModel.PatientExperimentOutputFolder);

                ////if(!File.Exists(savingFolder))
                ////    System.IO.Directory.CreateDirectory(savingFolder);

                // Debug 
                //MessageBox.Show(_patientStore.PrintPatient(), "Patient");
                #endregion

                // Crea DeviceStore 
                _deviceConnectionStore.CreateDeviceConnection(_devicePageViewModel.SelectedDevice);

                // Inizializzo un nuovo oggetto Esperimento
                // effettuando il retrieving parziale dei campi
                Experiment experiment = new Experiment()
                {
                    Patient = _devicePageViewModel.Patient,
                    Folder = "",
                    CurrentTask = 1,
                    TaskImagePath = "",
                    ImageFiles = new List<string>(),
                    TextFiles = new List<string>()
                };

                // Crea ExperimentStore
                _experimentStore.CreateExperiment(experiment);

                // Creo la stringa che rappresenterà il path in cui salvare i Task del paziente
                string savingFolder = App.GetAppFolder();
                savingFolder = Path.Combine(savingFolder, _devicePageViewModel.PatientExperimentOutputFolder);

                // Assegno il path della cartella dove salvare i file .csv all'oggetto Esperimento
                _experimentStore.SetExperimentFolder(savingFolder);

                // Assegno il path sorgente dei Task
                _experimentStore.SetExperimentTaskSourceFolder(_devicePageViewModel.TaskSourceFolder);

                // Creo la lista delle immagini presenti nella cartella Task selezionata
                _experimentStore.SetExperimentTaskListOrdered();

                // Creo la lista delle istruzioni per i Task presenti nella cartella  selezionata
                _experimentStore.SetExperimentTaskInstructionListOrdered();

                // Debug lista immagini Task
                //List<string> listA = new List<string>();

                //listA = _experimentStore.GetListTaskImage();

                //foreach (string file in listA)
                //    MessageBox.Show(file, "File");

                // Crea la Cartella in Documenti/Application_saving_folder/..
                Directory.CreateDirectory(savingFolder);

                // Apre la finestra nella quale verranno gestiti i Task
                _deviceWindow = new DeviceWindow(_deviceConnectionStore.Connection.InkDeviceInfo, _experimentStore);

                // Elimina memoria inutilizzata
                GC.Collect();

                // Apre la Finestra dei Task
                _deviceWindow.Show();

                //MessageBox.Show(_experimentStore.ExperimentTaskSourceFolder(), "Sorgente Esperimento");


                // Apro la finestra dei task in un nuovo thread
                //Task.Factory.StartNew(new Action(() =>
                //{
                //    _deviceWindow = new DeviceWindow(_deviceConnectionStore.Connection.InkDeviceInfo, _experimentStore);
                //    _deviceWindow.Show();
                //}), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

                // Naviga alla pagina successiva
                //_navigationService.Navigate();
            }

        }
    }
}
