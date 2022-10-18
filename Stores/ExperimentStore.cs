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
            ExperimentCreated?.Invoke(experiment);
            _experiment = experiment;
        }

        public string GetPatientID()
        {
            return _experiment.GetPatientCode();
        }

        public void IncrementCurrentTask()
        {
            _experiment.CurrentTask++;
        }

        public void SetExperimentFolder(string saveFolder)
        {
            _experiment.SetFolder(saveFolder);
        }

        public void SetExperimentTaskSourceFolder(string sourceTaskFolder)
        {
            _experiment.SetImagePath(sourceTaskFolder);
        }

        public string ExperimentTaskSourceFolder()
        {
            return _experiment.TaskImagePath;
        }

        public string ExperimentFolder()
        {
            return _experiment.Folder;
        }

        public string PrintExperimentPatient()
        {
            return _experiment.Patient.ToString();
        }

        public int CurrentExperimentTask()
        {
            return _experiment.CurrentTask;
        }

        public string GetCurrentPatient()
        {
            return _experiment.GetPatientName();
        }

        public void SetExperimentTaskListOrdered()
        {

            var files = Directory.GetFiles(_experiment.TaskImagePath);

            foreach (string filename in files)
            {
                if (Regex.IsMatch(filename, @"\.jpg$|\.png$"))
                {
                    _experiment.ImageFiles.Add(filename);
                }
            }

            var result = _experiment.ImageFiles.OrderBy(x => x.Length);

            _experiment.ImageFiles = new List<string>();

            foreach (string entry in result)
            {
                _experiment.ImageFiles.Add(entry);
            }
        }

        public List<string> GetListTaskImage()
        {
            return _experiment.ImageFiles;
        }


        public void SetExperimentTaskInstructionListOrdered()
        {
            string pathInstructions = _experiment.TaskImagePath + "\\Istruzioni";

            if (Directory.Exists(pathInstructions))
            {
                var files = Directory.GetFiles(pathInstructions);

                if ((_experiment.TextFiles != null) && (!_experiment.TextFiles.Any()))
                {
                    foreach (string filename in files)
                    {
                        if (Regex.IsMatch(filename, @"\.txt$"))
                        {
                            _experiment.TextFiles.Add(filename);
                        }
                    }
                }

                var result = _experiment.TextFiles.OrderBy(x => x.Length);

                _experiment.TextFiles = new List<string>();

                foreach (string entry in result)
                {
                    _experiment.TextFiles.Add(entry);
                }
            }
            else
            {
                _experiment.TextFiles = new List<string>();
            }


        }


        public List<string> GetListTaskInstruction()
        {
            return _experiment.TextFiles;
        }



    }
}
