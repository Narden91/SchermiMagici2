using System.Collections.Generic;

namespace WpfApp1.Models
{
    /// <summary>
    /// Classe modello per l'Esperimento
    /// </summary>
    public class Experiment
    {
        public Patient Patient { get; set; }
        public string Folder { get; set; }
        public int CurrentTask { get; set; }
        public string TaskImagePath { get; set; }
        public List<string> ImageFiles { get; set; }

        public List<string> TextFiles { get; set; }


        public void SetFolder(string saveFolder)
        {
            Folder = saveFolder;
        }

        public void SetImagePath(string imagePath)
        {
            TaskImagePath = imagePath;
        }


        public string GetPatientCode()
        {
            return Patient.PatientId;
        }


        public void PatientPrint()
        {
            Patient.ToString();
        }


        public string ExperimentFolderPrint()
        {
            return Folder.ToString();
        }

        public string GetPatientName()
        {
            return Patient.Name + " " + Patient.Surname;
        }
    }
}
