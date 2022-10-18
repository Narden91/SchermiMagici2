using System;
using WpfApp1.Models;

namespace WpfApp1.Stores
{
    public class ProctorStore
    {
        private Proctor? _proctor;

        public event Action<Proctor> PatientCreated;


        /// <summary>
        /// Crea una nuova istanza con le informazioni del Somministratore
        /// </summary>
        /// <param name="proctor"></param>
        public void CreateProctor(Proctor proctor)
        {
            PatientCreated?.Invoke(proctor);
            _proctor = proctor;
        }

        /// <summary>
        /// Genera un identificativo per la cartella in cui salvare 
        /// i task che verranno eseguiri dal paziente, sulla base delle informazioni
        /// del Somministratore.
        /// </summary>
        /// <returns></returns>
        public string UniqueCodeGenerator(string patientId)
        {
            return _proctor.Name.Substring(0, 2) + "_" +
                    _proctor.Surname.Substring(0, 2) + "_" +
                    _proctor.City + "_" +
                    patientId;
        }


        public string IdPatientCodeGenerator()
        {
            return DateTime.Now.Day.ToString() + "_" +
                    DateTime.Now.Month.ToString() + "_" +
                    DateTime.Now.Year.ToString() + "_" +
                    DateTime.Now.Hour.ToString() + "_" +
                    DateTime.Now.Minute.ToString() + "_" +
                    DateTime.Now.Second.ToString();
        }

        public string GetTaskImagesPath()
        {
            return _proctor.TaskImagesPath;
        }


        public string PrintProctor()
        {
            return _proctor.ToString();
        }
    }
}