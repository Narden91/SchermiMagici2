using System;

namespace WpfApp1.Models
{
    public class Patient
    {
        public string? PatientId { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? Gender { get; set; }
        public string? Birthdate { get; set; }
        public string? Dominanthand { get; set; }
        public string? Edclass { get; set; }
        public string? DiagnosedDiseases { get; set; }
        public string? Notes { get; set; }
        public string? PatientOutputDataFolder { get; set; }
        public string? TaskPathFolder { get; set; }


        public override string ToString()
        {
            return "ID: " + this.PatientId + Environment.NewLine +
                   "Nome: " + this.Name + Environment.NewLine +
                   "Cognome: " + this.Surname + Environment.NewLine +
                   "Sesso: " + this.Gender + Environment.NewLine +
                   "Data di Nascita: " + this.Birthdate + Environment.NewLine +
                   "Mano Dominante: " + this.Dominanthand + Environment.NewLine +
                   "Classe: " + this.Edclass + Environment.NewLine +
                   "Malattie Diagnosticate: " + this.DiagnosedDiseases + Environment.NewLine +
                   "Note: " + this.Notes + Environment.NewLine +
                   "Cartella salvataggio: " + this.PatientOutputDataFolder + Environment.NewLine +
                   "Cartella sorgente Task: " + this.TaskPathFolder + Environment.NewLine;
        }
    }
}
