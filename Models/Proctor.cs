using System;

namespace WpfApp1.Models
{
    public class Proctor
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? City { get; set; }
        public string? Notes { get; set; }
        public string? TaskImagesPath { get; set; }

        public override string ToString()
        {
            return "Nome: " + this.Name + Environment.NewLine +
                   "Cognome: " + this.Surname + Environment.NewLine +
                   "Città: " + this.City + Environment.NewLine +
                   "Task Path: " + this.TaskImagesPath + Environment.NewLine +
                   "Note: " + this.Notes + Environment.NewLine;
        }
    }
}
