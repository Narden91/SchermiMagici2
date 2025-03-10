using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WpfApp1.Models;

namespace WpfApp1.Stores
{
    /// <summary>
    /// Classe per mantenere le informazioni sull'esperimento 
    /// attualmente in corso
    /// </summary>
    public class ExperimentStore
    {
        private Experiment _experiment;

        public Experiment Experiment => _experiment;

        public event Action<Experiment> ExperimentCreated;

        public void CreateExperiment(Experiment experiment)
        {
            if (experiment == null)
            {
                throw new ArgumentNullException(nameof(experiment), "L'oggetto esperimento non può essere nullo.");
            }

            ExperimentCreated?.Invoke(experiment);
            _experiment = experiment;
        }

        public string GetPatientID()
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }
            return _experiment.GetPatientCode();
        }

        public void IncrementCurrentTask()
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }
            _experiment.CurrentTask++;
        }

        public void SetExperimentFolder(string saveFolder)
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }

            if (string.IsNullOrEmpty(saveFolder))
            {
                throw new ArgumentException("Il percorso di salvataggio non può essere vuoto.", nameof(saveFolder));
            }

            _experiment.SetFolder(saveFolder);
        }

        public void SetExperimentTaskSourceFolder(string sourceTaskFolder)
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }

            if (string.IsNullOrEmpty(sourceTaskFolder))
            {
                throw new ArgumentException("Il percorso sorgente dei task non può essere vuoto.", nameof(sourceTaskFolder));
            }

            if (!Directory.Exists(sourceTaskFolder))
            {
                throw new DirectoryNotFoundException($"La directory dei task '{sourceTaskFolder}' non esiste.");
            }

            _experiment.SetImagePath(sourceTaskFolder);
        }

        public string ExperimentTaskSourceFolder()
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }
            return _experiment.TaskImagePath;
        }

        public string ExperimentFolder()
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }
            return _experiment.Folder;
        }

        public string PrintExperimentPatient()
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }
            return _experiment.Patient.ToString();
        }

        public int CurrentExperimentTask()
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }
            return _experiment.CurrentTask;
        }

        public string GetCurrentPatient()
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }
            return _experiment.GetPatientName();
        }

        public void SetExperimentTaskListOrdered()
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }

            if (string.IsNullOrEmpty(_experiment.TaskImagePath))
            {
                throw new InvalidOperationException("Il percorso delle immagini dei task non è stato impostato.");
            }

            if (!Directory.Exists(_experiment.TaskImagePath))
            {
                throw new DirectoryNotFoundException($"La directory dei task '{_experiment.TaskImagePath}' non esiste.");
            }

            try
            {
                // Inizializza la lista di file se è null
                if (_experiment.ImageFiles == null)
                {
                    _experiment.ImageFiles = new List<string>();
                }
                else
                {
                    // Svuota la lista se già esistente
                    _experiment.ImageFiles.Clear();
                }

                var files = Directory.GetFiles(_experiment.TaskImagePath);

                foreach (string filename in files)
                {
                    if (Regex.IsMatch(filename, @"\.jpg$|\.png$", RegexOptions.IgnoreCase))
                    {
                        _experiment.ImageFiles.Add(filename);
                    }
                }

                if (_experiment.ImageFiles.Count == 0)
                {
                    throw new FileNotFoundException("Non sono state trovate immagini (jpg o png) nella cartella dei task.");
                }

                var result = _experiment.ImageFiles.OrderBy(x => x.Length);
                _experiment.ImageFiles = new List<string>(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException($"Accesso negato alla directory dei task: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                throw new IOException($"Errore di I/O durante la lettura dei file dei task: {ex.Message}", ex);
            }
        }

        public List<string> GetListTaskImage()
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }

            if (_experiment.ImageFiles == null)
            {
                return new List<string>();
            }

            return new List<string>(_experiment.ImageFiles);
        }

        public void SetExperimentTaskInstructionListOrdered()
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }

            if (string.IsNullOrEmpty(_experiment.TaskImagePath))
            {
                throw new InvalidOperationException("Il percorso delle immagini dei task non è stato impostato.");
            }

            try
            {
                // Inizializza la lista di istruzioni se è null
                if (_experiment.TextFiles == null)
                {
                    _experiment.TextFiles = new List<string>();
                }
                else
                {
                    // Svuota la lista se già esistente
                    _experiment.TextFiles.Clear();
                }

                string pathInstructions = Path.Combine(_experiment.TaskImagePath, "Istruzioni");

                if (Directory.Exists(pathInstructions))
                {
                    var files = Directory.GetFiles(pathInstructions);

                    foreach (string filename in files)
                    {
                        if (Regex.IsMatch(filename, @"\.txt$", RegexOptions.IgnoreCase))
                        {
                            _experiment.TextFiles.Add(filename);
                        }
                    }

                    var result = _experiment.TextFiles.OrderBy(x => x.Length);
                    _experiment.TextFiles = new List<string>(result);
                }
                // Se la cartella non esiste, _experiment.TextFiles rimane una lista vuota
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException($"Accesso negato alla directory delle istruzioni: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                throw new IOException($"Errore di I/O durante la lettura dei file delle istruzioni: {ex.Message}", ex);
            }
        }

        public List<string> GetListTaskInstruction()
        {
            if (_experiment == null)
            {
                throw new InvalidOperationException("Nessun esperimento attualmente attivo.");
            }

            if (_experiment.TextFiles == null)
            {
                return new List<string>();
            }

            return new List<string>(_experiment.TextFiles);
        }
    }
}