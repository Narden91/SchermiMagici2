using System;
using WpfApp1.Models;

namespace WpfApp1.Stores
{
    public class PatientStore
    {
        private Patient? _patient;

        public Patient Patient => _patient;

        public event Action<Patient> PatientCreated;

        public void CreatePatient(Patient patient)
        {
            PatientCreated?.Invoke(patient);
            _patient = patient;
        }

        public void SetPatientFolder(string folder)
        {
            _patient.PatientOutputDataFolder = folder;
        }

        public void SetTaskSourceFolder(string folder)
        {
            _patient.TaskPathFolder = folder;
        }

        public void SetPatientID(string idcode)
        {
            _patient.PatientId = idcode;
        }

        public string PrintPatient()
        {
            return _patient.ToString();
        }

    }
}
