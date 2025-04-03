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

		public override string ToString() //************ ???? ************
		{
			return $"[{Type}] {Date:G} : {(Type == "Dépôt" ? "+" : "-")}{Montant:C}. Nouveau solde : {NouveauSolde:C}";
		}
	}
}