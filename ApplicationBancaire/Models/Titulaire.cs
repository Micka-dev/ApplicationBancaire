using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ApplicationBancaire.Models
{
	// La classe Titulaire regroupe toutes les données d'un client
	public class Titulaire
	{
		public required string Identifiant { get; set; }
		public required string MotDePasse { get; set; }
		public required string Prenom { get; set; }
		public required string Nom { get; set; }
		public required string NumeroCompte { get; set; }
		public required string Iban { get; set; }
		public required decimal SoldeCompteCourant { get; set; }
		public required decimal SoldeCompteEpargne { get; set; }
		public List<Transaction> TransactionsCompteCourant { get; set; } = new List<Transaction>();
		public List<Transaction> TransactionsCompteEpargne { get; set; } = new List<Transaction>();

		// Utilisation de [JsonInclude] pour persister le marqueur. Cela permettra de mémoriser jusqu'où les transactions ont déjà été écrites (indice).
		[JsonInclude] public int DernierIndiceTransactionsCompteCourant = 0;
		[JsonInclude] public int DernierIndiceTransactionsCompteEpargne = 0;

		//Liste des bénéficiaires enregistrés pour ce client.
		public List<Beneficiaire> Beneficiaires { get; set; } = new List<Beneficiaire>();
	}
}
