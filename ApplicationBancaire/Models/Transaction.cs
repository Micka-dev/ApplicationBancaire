using System;

namespace ApplicationBancaire.Models
{
	// Classe Transaction regroupant une opération bancaire
	public class Transaction
	{
		public DateTime Date { get; set; }
		public required string Type { get; set; }  // "Dépôt" ou "Retrait"
		public decimal Montant { get; set; }
		public decimal NouveauSolde { get; set; }

        public override string ToString()
        {
            // Déterminer le symbole à afficher en fonction du type d'opération.
            string symbol;
            if (Type.IndexOf("entrant", StringComparison.OrdinalIgnoreCase) >= 0 || Type.Equals("Dépôt", StringComparison.OrdinalIgnoreCase))
            {
                symbol = "+";
            }
            else if (Type.IndexOf("sortant", StringComparison.OrdinalIgnoreCase) >= 0 || Type.Equals("Retrait", StringComparison.OrdinalIgnoreCase))
            {
                symbol = "-";
            }
            else
            {
                // Par défaut, on peut laisser vide ou choisir un comportement
                symbol = "";
            }

            return $"[{Type}] {Date:G} : {symbol}{Montant:C}. Nouveau solde : {NouveauSolde:C}";
        }
    }
}