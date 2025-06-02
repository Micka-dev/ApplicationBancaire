namespace ApplicationBancaire.Models
{
    // Classe pour encapsuler le résultat de saisie
    public class SaisieResultat
    {
        public bool Abandon { get; set; } // Indique si l'utilisateur a abandonné
        public string? Valeur { get; set; } // La saisie de l'utilisateur
        public string? Message { get; set; } // Message complémentaire
    }

}
