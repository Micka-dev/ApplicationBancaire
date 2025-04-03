// Models/Beneficiaire.cs
using System;

namespace ApplicationBancaire.Models
{
    public class Beneficiaire
    {
        public string Identifiant { get; set; } = "";
        public string Nom { get; set; } = "";
        public string Prenom { get; set; } = "";
        public string NumeroCompte { get; set; } = "";
        public required string Iban { get; set; } = "";

        public override string ToString()
        {
            return $"{Prenom} {Nom} - IBAN: {Iban}";
        }
    }
}
