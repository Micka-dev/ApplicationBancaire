namespace ApplicationBancaire.Models
{
    public class MontantResultat
    {
        public bool Abandon { get; set; } // Indique si l'utilisateur a abandonné
        public decimal? Montant { get; set; } // Le montant saisi par l'utilisateur
    }

}