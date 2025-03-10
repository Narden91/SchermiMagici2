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
        private readonly DevicePageViewModel _devicePageViewModel;
        private readonly ExperimentStore _experimentStore;
        private readonly DeviceConnectionStore _deviceConnectionStore;

        private DeviceWindow _deviceWindow;

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
                MessageBox.Show("Nessun dispositivo selezionato. Per favore, seleziona un dispositivo prima di continuare.",
                              "Dispositivo Mancante", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
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
                string savingFolder = null;
                try
                {
                    savingFolder = App.GetAppFolder();
                    if (string.IsNullOrEmpty(savingFolder))
                    {
                        throw new DirectoryNotFoundException("Il percorso di salvataggio dell'applicazione non è valido.");
                    }

                    savingFolder = Path.Combine(savingFolder, _devicePageViewModel.PatientExperimentOutputFolder);
                    if (savingFolder.Length >= 248) // Windows path length limitation
                    {
                        throw new PathTooLongException("Il percorso di salvataggio è troppo lungo. Scegli un nome più breve per il paziente.");
                    }
                }
                catch (Exception ex) when (ex is ArgumentException || ex is PathTooLongException)
                {
                    MessageBox.Show($"Errore nella creazione del percorso: {ex.Message}",
                                  "Errore Percorso", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Assegno il path della cartella dove salvare i file .csv all'oggetto Esperimento
                _experimentStore.SetExperimentFolder(savingFolder);

                try
                {
                    // Verifica se la cartella sorgente dei Task esiste
                    if (!Directory.Exists(_devicePageViewModel.TaskSourceFolder))
                    {
                        throw new DirectoryNotFoundException("La cartella sorgente dei Task non esiste.");
                    }

                    // Assegno il path sorgente dei Task
                    _experimentStore.SetExperimentTaskSourceFolder(_devicePageViewModel.TaskSourceFolder);
                }
                catch (DirectoryNotFoundException ex)
                {
                    MessageBox.Show($"Errore nella cartella sorgente dei Task: {ex.Message}",
                                  "Errore Configurazione", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore imprevisto durante la configurazione del Task: {ex.Message}",
                                  "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    // Creo la lista delle immagini presenti nella cartella Task selezionata
                    _experimentStore.SetExperimentTaskListOrdered();

                    // Creo la lista delle istruzioni per i Task presenti nella cartella selezionata
                    _experimentStore.SetExperimentTaskInstructionListOrdered();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore durante la preparazione dei Task: {ex.Message}",
                                  "Errore Task", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    // Verifica se la directory esiste già
                    if (Directory.Exists(savingFolder))
                    {
                        // Opzionale: chiedere all'utente se sovrascrivere
                        MessageBoxResult result = MessageBox.Show(
                            "La cartella di output già esiste. Vuoi utilizzarla comunque?",
                            "Cartella esistente",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }
                    }
                    else
                    {
                        // Crea la Cartella in Documenti/Application_saving_folder/..
                        Directory.CreateDirectory(savingFolder);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Non hai i permessi necessari per creare la cartella di destinazione. " +
                                  "Prova ad eseguire l'applicazione come amministratore o scegli un'altra posizione.",
                                  "Errore di Permessi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Errore di I/O durante la creazione della cartella: {ex.Message}",
                                  "Errore I/O", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore imprevisto durante la creazione della cartella: {ex.Message}",
                                  "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    // Apre la finestra nella quale verranno gestiti i Task
                    _deviceWindow = new DeviceWindow(_deviceConnectionStore.Connection.InkDeviceInfo, _experimentStore);

                    // Apre la Finestra dei Task
                    _deviceWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore durante l'apertura della finestra Task: {ex.Message}",
                                  "Errore UI", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Si è verificato un errore imprevisto: {ex.Message}",
                              "Errore Critico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}