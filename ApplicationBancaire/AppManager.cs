
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Text.Json.Serialization;

using ApplicationBancaire.Models;


namespace ApplicationBancaire
{
    public static class AppManager
    {
        #region Champs et Initialisation

        // Liste complète de titulaires
        private static List<Titulaire> titulaires = new List<Titulaire>();

        // currentUser représente l'utilisateur authentifié
        private static Titulaire currentUser = default!;


        public static void Run()
        {
            // Configuration de la console et de la culture.
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            ChargerTitulairesDepuisJson();
            AuthentifierUtilisateur();
            AttendreToucheEntrer();
            LancerMenuPrincipal();
        }
        #endregion

        #region Méthodes Utilitaires communes

        // Centraliser la lecture d'une entrée utilisateur non vide (sans option quitter)
        //private static string LireEntreeNonVide(string invite)
        //{
        //    while (true)
        //    {
        //        Console.Write(invite);
        //        string? input = Console.ReadLine();
        //        if (!string.IsNullOrWhiteSpace(input))
        //            return input.Trim();
        //        AfficherErreur("Entrée invalide. Veuillez réessayer.\n");
        //    }
        //}

        // Centraliser la lecture d'une entrée utilisateur non vide avec OPTION QUITTER
        private static SaisieResultat LireEntreeAvecOptionQuitter(string message, string contexte)
        {
            while (true)
            {
                AfficherConsigneQuitter(contexte);
                Console.Write(message);
                string entree = Console.ReadLine()?.Trim() ?? "";

                if (entree.ToUpper() == "Q") // L'utilisateur souhaite abandonner
                {
                    Console.WriteLine($"Abandon en cours... {contexte}");
                    return new SaisieResultat
                    {
                        Abandon = true,
                        Message = "L'utilisateur a choisi d'abandonner."
                    };
                }

                if (!string.IsNullOrWhiteSpace(entree)) // Une saisie valide a été réalisée
                {
                    return new SaisieResultat
                    {
                        Abandon = false,
                        Valeur = entree,
                        Message = "Saisie valide."
                    };
                }

                // Afficher une erreur si la saisie est vide
                AfficherErreur("Saisie invalide. Veuillez réessayer.");
            }
        }

        // Méthode de validation de saisie (nom, prénom, IBAN)
        private static bool ValiderNomPrenom(string nomOuPrenom)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(nomOuPrenom, @"^[A-Za-zÀ-ÖØ-öø-ÿ\s'-]+$");
        }

        private static bool ValiderIban(string iban)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(iban, @"^[A-Z]{2}\d{2}[A-Z0-9]{23}$");
        }

        // Méthode pour reformater l'iban
        private static string ReformaterIban(string iban)
        {
            // Supprime tous les espaces ou caractères inutiles
            string ibanNettoye = NormaliserIbanPourRecherche(iban);

            // Ajoute des espaces tous les 4 caractères pour formater l'IBAN
            return string.Join(" ", Enumerable.Range(0, ibanNettoye.Length / 4 + (ibanNettoye.Length % 4 > 0 ? 1 : 0))
                                           .Select(i => ibanNettoye.Substring(i * 4, Math.Min(4, ibanNettoye.Length - i * 4))));
        }

        private static string NormaliserIbanPourRecherche(string iban)
        {
            // Supprime les espaces pour des comparaisons standarisées
            return iban.Replace(" ", "").Trim();
        }

        // Méthode pour enregistrer un nouveau bénéficiaire
        private static Beneficiaire? EnregistrerNouveauBeneficiaire()
        {
            string nomBenef;
            do
            {
                SaisieResultat resultatNom = LireEntreeAvecOptionQuitter("Entrez le nom du bénéficiaire : ", "revenir au menu principal.");
                if (resultatNom.Abandon) return null; // L'utilisateur a choisi de quitter
                nomBenef = resultatNom.Valeur!;

                if (!ValiderNomPrenom(nomBenef))
                {
                    AfficherErreur("Nom invalide. Assurez-vous de n'utiliser que des lettres, espaces ou apostrophes.");

                }
            }
            while (!ValiderNomPrenom(nomBenef));

            string prenomBenef;
            do
            {
                SaisieResultat resultatPrenom = LireEntreeAvecOptionQuitter("Entrez le prénom du bénéficiaire : ", "revenir au menu principal.");
                if (resultatPrenom.Abandon) return null;
                prenomBenef = resultatPrenom.Valeur!;

                if (!ValiderNomPrenom(prenomBenef))
                {
                    AfficherErreur("Prénom invalide. Assurez-vous de n'utiliser que des lettres, espaces ou apostrophes.");
                }
            }
            while (!ValiderNomPrenom(prenomBenef));

            string ibanBenef;
            do
            {
                SaisieResultat resultatIban = LireEntreeAvecOptionQuitter("Entrez l'IBAN du bénéficiaire : ", "revenir au menu principal.");
                if (resultatIban.Abandon) return null;

                ibanBenef = resultatIban.Valeur!;

                //Normaliser l'IBAN pour comparaison ou validation
                string ibanNormalise = NormaliserIbanPourRecherche(ibanBenef);

                if (!ValiderIban(ibanNormalise))
                {
                    AfficherErreur("IBAN invalide. Assurez-vous de fournir un IBAN correct au format standard (ex : FR76...).");
                }
            }
            while (!ValiderIban(NormaliserIbanPourRecherche(ibanBenef)));

            // Vérifie que l'utilisateur ne s'ajoute pas lui-même
            if (currentUser.Nom.Equals(nomBenef, StringComparison.OrdinalIgnoreCase) &&
                    currentUser.Prenom.Equals(prenomBenef, StringComparison.OrdinalIgnoreCase))
            {
                AfficherErreur("Vous ne pouvez pas vous enregistrer vous-même comme bénéficiaire.");
                return null;
            }

            // Recherche du titulaire correspondant à l'IBAN
            Titulaire? benefTitulaire = titulaires.FirstOrDefault(t => NormaliserIbanPourRecherche(t.Iban) == NormaliserIbanPourRecherche(ibanBenef)); // Comparaison normalisée

            if (benefTitulaire != null)
            {
                return new Beneficiaire
                {
                    Identifiant = benefTitulaire.Identifiant,
                    Nom = benefTitulaire.Nom,
                    Prenom = benefTitulaire.Prenom,
                    Iban = ReformaterIban(benefTitulaire.Iban),
                    NumeroCompte = benefTitulaire.NumeroCompte
                };
            }
            else
            {
                return new Beneficiaire
                {
                    Nom = nomBenef,
                    Prenom = prenomBenef,
                    Iban = ibanBenef,
                    NumeroCompte = ""
                };
            }
        }

        // Méthode pour enregistrer une transaction
        private static void AddTransaction(List<Transaction> transactions, string operationType, decimal montant, decimal nouveauSolde)
        {
            transactions.Add(new Transaction
            {
                Date = DateTime.Now,
                Type = operationType,
                Montant = montant,
                NouveauSolde = nouveauSolde
            });
        }

        // Centraliser l'affichage d'erreurs en rouge
        private static void AfficherErreur(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        // Afficher message pour abandonner
        private static void AfficherConsigneQuitter(string contexte)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Note : Vous pouvez appuyer sur 'Q' pour abandonner et {contexte}.\n");
            Console.ResetColor();
        }

        // Méthode pour afficher un en-tête formaté
        //private static void DisplayHeader(string header)
        //{
        //    Console.ForegroundColor = ConsoleColor.Cyan;
        //    Console.WriteLine(header + "\n");
        //    Console.ResetColor();
        //}

        // Méthode d'attente pour que l'utilisateur lise les affichages
        private static void AttendreToucheEntrer()
        {
            Console.WriteLine("Appuyez sur 'Entrée' pour accéder au menu");
            while (Console.ReadKey(intercept: true).Key != ConsoleKey.Enter)
            {
                AfficherErreur("Ce n'est pas la touche Entrée, réessayez !");
            }
        }
        #endregion

        #region Gestion des Données JSON

        // Chargement des titulaires depuis "titulaires.json"
        private static void ChargerTitulairesDepuisJson()
        {
            string filePath = "titulaires.json";
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    titulaires = JsonSerializer.Deserialize<List<Titulaire>>(json)
                    ?? throw new InvalidOperationException("Le fichier JSON est vide ou contient des données invalides.");

                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("Les titulaires ont été chargés depuis le fichier JSON.");
                    Console.ResetColor();
                }
                else
                {
                    AfficherErreur("Fichier 'titulaires.json' introuvable. Aucun titulaire n'a été chargé.");
                }
            }
            catch (Exception ex)
            {
                AfficherErreur($"Erreur lors du chargement des titulaires : {ex.Message}");
            }
        }

        // Sauvegarde l'ensemble des titulaires dans le fichier JSON
        private static void SauvegarderTitulairesEnJson()
        {
            string filePath = "titulaires.json";
            try
            {
                string json = JsonSerializer.Serialize(titulaires, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Les données des titulaires ont été sauvegardées dans le fichier JSON.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                AfficherErreur($"Erreur lors de la sauvegarde des titulaires : {ex.Message}");
            }
        }
        #endregion

        #region Authentification
        private static void AuthentifierUtilisateur()
        {
            // Nombre maximum de tentatives autorisées
            const int maxTentatives = 5;
            int tentative = 0;

            // Récupération de l'utilisateur par son identifiant
            Titulaire? utilisateurTrouve = ObtenirUtilisateurParIdentifiant();

            // Tenter l'authentification en demandant le mot de passe (boucle jusqu'à succès ou dépassement du nombre max de tentatives)
            while (!AuthentifierMotDePasse(utilisateurTrouve))
            {
                tentative++;
                int restant = maxTentatives - tentative;

                // Message d'avertissement si 2 ou 1 tentative(s) restante(s)
                if (restant == 2)
                {
                    AfficherErreur("Attention, il ne vous reste que 2 tentatives !");
                }
                else if (restant == 1)
                {
                    AfficherErreur("Attention, il ne vous reste que 1 tentative !");
                }

                // Vérifier si le nombre maximal de tentatives est atteint.
                if (tentative >= maxTentatives)
                {
                    AfficherErreur("Nombre de tentatives maximal atteint. L'application va quitter.");
                    AttendreToucheEntrer();
                    Environment.Exit(1);
                }
                AfficherErreur("Mot de passe incorrect. Veuillez réessayer.\n");
            }

            // Affichage d'un message de bienvenue
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"\nBienvenue {currentUser.Prenom} {currentUser.Nom} !");
            Console.ResetColor();
        }

        // Obtention d'un utilisateur par identifiant
        private static Titulaire ObtenirUtilisateurParIdentifiant()
        {
            Titulaire? utilisateurTrouve = null;

            while (utilisateurTrouve == null)
            {
                SaisieResultat resultat = LireEntreeAvecOptionQuitter("Entrez votre identifiant : ", "quitter l'application.");
                if (resultat.Abandon)
                {
                    Environment.Exit(0);
                }
                string identifiantSaisi = resultat.Valeur!;

                utilisateurTrouve = titulaires.FirstOrDefault(t => t.Identifiant == identifiantSaisi);
                if (utilisateurTrouve == null)
                {
                    AfficherErreur("L'identifiant n'existe pas. Veuillez réessayer.\n");
                }
            }

            return utilisateurTrouve;
        }

        // Vérification du mot de passe pour l'utilisateur
        private static bool AuthentifierMotDePasse(Titulaire utilisateur)
        {
            SaisieResultat resultat = LireEntreeAvecOptionQuitter("Entrez votre mot de passe : ", "quitter l'application.");
            if (resultat.Abandon) Environment.Exit(0);
            string motDePasseSaisi = resultat.Valeur!;

            if (utilisateur.MotDePasse == motDePasseSaisi)
            {
                currentUser = utilisateur;
                return true; // Mot de passe correct
            }

            return false; // Mot de passe incorrect
        }
        #endregion

        #region Menu Principal et Options

        // Affichage du menu avec mise en forme et couleurs
        private static void AfficherMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================== Vous êtes sur le menu de votre application bancaire ! ========================\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Veuillez entrer le code correspondant au service et valider avec 'Entrée' pour y accéder.");
            Console.WriteLine("(Par exemple, taper 'I' pour accéder à vos informations)\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("------------ Informations sur le titulaire -------------");
            Console.ResetColor();
            Console.WriteLine("[ I ] Consulter vos informations.");
            Console.WriteLine("[ MP ] Modifier votre mot de passe\n");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("----------------- Opérations Bancaires -----------------\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("----------- Compte Courant => Retraits/Dépôts ----------");
            Console.ResetColor();
            Console.WriteLine("[ CS ] Consulter le solde de votre compte courant.");
            Console.WriteLine("[ CD ] Déposer sur votre compte courant.");
            Console.WriteLine("[ CR ] Retirer du compte courant.\n");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("----------- Compte Epargne => Retraits/Dépôts ----------");
            Console.ResetColor();
            Console.WriteLine("[ ES ] Consulter le solde de votre compte épargne.");
            Console.WriteLine("[ ED ] Déposer sur votre compte épargne.");
            Console.WriteLine("[ ER ] Retirer du compte épargne.\n");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("----------------------- Virements ----------------------");
            Console.ResetColor();
            Console.WriteLine("[ VI ] Effectuer un virement interne.");
            Console.WriteLine("[ VE ] Effectuer un virement externe.");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("--------------------------------------------------------");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("----------------- Quitter l'application ----------------");
            Console.WriteLine("[ X ] Quitter l'application.\n");
            Console.ResetColor();
        }

        private static void LancerMenuPrincipal()
        {
            var options = new Dictionary<string, Action>
            {
                { "I", AfficherInformationsTitulaire },
                { "MP", ModifierMotDePasse },
                { "CS", () => ConsulterSolde("compte courant", currentUser.SoldeCompteCourant, currentUser.TransactionsCompteCourant) },
                { "CD", DeposerCompteCourant },
                { "CR", RetirerCompteCourant },
                { "ES", () => ConsulterSolde("compte épargne", currentUser.SoldeCompteEpargne, currentUser.TransactionsCompteEpargne) },
                { "ED", DeposerCompteEpargne },
                { "ER", RetirerCompteEpargne },
                { "VI", VirementInterne },
                { "VE", VirementExterne },
                { "X", QuitterApplication }
            };

            while (true)
            {
                AfficherMenu();
                Console.Write("\nEntrez votre choix : ");
                string input = Console.ReadLine() ?? "";
                if (string.IsNullOrWhiteSpace(input))
                {
                    AfficherErreur("Aucune entrée détectée. Veuillez réessayer.");
                    AttendreToucheEntrer();
                    continue;
                }

                string choix = input.ToUpper();
                if (options.ContainsKey(choix))
                {
                    options[choix]();
                    if (choix == "X")
                        break;
                }
                else
                {
                    AfficherErreur("Option invalide. Veuillez réessayer.");
                    AttendreToucheEntrer();
                }
            }
        }
        #endregion

        #region Consultation du Solde et Historique des transactions

        // Méthode commune pour consulter un solde (compte courant ou épargne)
        private static void ConsulterSolde(string compte, decimal solde, List<Transaction> historique)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n--- Consultation du solde du {compte}. ---\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine($"Le solde de votre {compte.ToLower()} est de {solde:C}.\n");
            Console.ResetColor();

            do
            {
                SaisieResultat resultatHistorique = LireEntreeAvecOptionQuitter("Voulez-vous afficher l'historique des transactions ? (O/N) : ", "revenir au menu principal.");
                if (resultatHistorique.Abandon) return; // Quitte immédiatement et retourne au menu principal

                if (resultatHistorique.Valeur?.Trim().ToUpper() == "N") return;

                if (resultatHistorique.Valeur?.Trim().ToUpper() == "O")
                {
                    Console.WriteLine($"\nHistorique des transactions pour le {compte} (du plus récent au plus ancien):\n");
                    foreach (var transaction in historique.AsEnumerable().Reverse())
                    {
                        Console.WriteLine(transaction.ToString());
                    }
                    Console.WriteLine("\n\n");
                    AttendreToucheEntrer();
                    return;
                }
                else { AfficherErreur("Saisie incorrecte. Veuillez réessayer !"); }
            } while (true);

        }
        #endregion

        #region Modification du mot de passe

        private static void ModifierMotDePasse()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("--- Modification du Mot de Passe ---\n");
            Console.ResetColor();

            // Vérification du mot de passe actuel.
            SaisieResultat resultatActuel = LireEntreeAvecOptionQuitter("Entrez votre mot de passe actuel : ", "revenir au menu principal.");
            if (resultatActuel.Abandon) return;
            string actuel = resultatActuel.Valeur!;

            if (actuel != currentUser.MotDePasse)
            {
                AfficherErreur("Mot de passe actuel incorrect.");
                AttendreToucheEntrer();
                return;
            }

            // Saisie du nouveau mot de passe avec confirmation.
            SaisieResultat resultatNouveau = LireEntreeAvecOptionQuitter("Entrez votre nouveau mot de passe : ", "revenir au menu principal.");
            if (resultatNouveau.Abandon) return;
            string nouveau = resultatNouveau.Valeur!;

            SaisieResultat resultatConfirmation = LireEntreeAvecOptionQuitter("Confirmez votre nouveau mot de passe : ", "revenir au menu principal.");
            if (resultatConfirmation.Abandon) return;
            string confirmation = resultatConfirmation.Valeur!;

            if (nouveau != confirmation)
            {
                AfficherErreur("Les deux saisies ne correspondent pas.");
                AttendreToucheEntrer();
                return;
            }

            currentUser.MotDePasse = nouveau;
            SauvegarderTitulairesEnJson();

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Mot de passe modifié avec succès !");
            Console.ResetColor();
            AttendreToucheEntrer();
        }
        #endregion

        #region Gestion des Montants

        private static MontantResultat ObtenirMontant(string message, decimal maxValue)
        {
            while (true)
            {
                // ICI METHODE POUR QUITTER : Saisie avec possibilité d'abandon
                SaisieResultat res = LireEntreeAvecOptionQuitter(message, "annuler l'opération et revenir au menu principal.");
                if (res.Abandon)
                {
                    return new MontantResultat { Abandon = true }; // L'utilisateur a choisi d'abandonner
                }

                string input = res.Valeur!;
                // Conversion d'une chaîne en montant (en tenant compte du format français et des autres)
                if (decimal.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out decimal montant) ||
                    decimal.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out montant))
                {
                    if (montant > 0 && montant <= maxValue)
                    {
                        return new MontantResultat { Abandon = false, Montant = montant }; // Montant valide
                    }

                    if (montant > maxValue)
                    {
                        AfficherErreur($"Veuillez entrer un montant inférieur ou égal à {maxValue:C}.\n");
                    }
                    else
                    {
                        AfficherErreur("Le montant doit être supérieur à 0.\n");
                    }
                }
                else
                {
                    AfficherErreur("Entrée invalide. Veuillez réessayer.\n");
                }
            }
        }

        #endregion

        #region Générateurs d'Affichage pour Opérations bancaires

        private static Action GenererAffichageTitreTransaction(string compte, string operation)
        {
            return () =>
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n--- {operation} sur le {compte}. ---\n");
                Console.ResetColor();
            };
        }

        private static Action GenererAffichageMontantOperation(string typeOperation, decimal montant)
        {
            return () =>
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(
                    typeOperation == "Dépôt"
                        ? $"La somme déposée est de : {montant:C}"
                        : $"La somme retirée est de : {montant:C}"
                );
                Console.ResetColor();
            };
        }

        private static Action GenererAffichageSoldeAJour(string compte, Func<decimal> obtenirSolde)
        {
            return () =>
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine($"Votre nouveau solde pour le {compte} est de : {obtenirSolde():C}\n");
                Console.ResetColor();
            };
        }
        #endregion

        #region Traitement Général d'une Opération (Dépôt/Retrait)

        private static void TraiterOperation(
            string typeOperation,
            string compte,
            string message,
            decimal maxValue,
            Action<decimal> miseAJourSolde,
            Func<decimal> obtenirSolde,
            List<Transaction> listeTransactions
        )
        {
            GenererAffichageTitreTransaction(compte, typeOperation)();

            // Lecture et validation du montant
            MontantResultat resultatMontant = ObtenirMontant(message, maxValue);
            if (resultatMontant.Abandon) return; // Quitte immédiatement et retourne au menu principal

            decimal montant = resultatMontant.Montant!.Value; // Assuré non nul car Abandon = false
            GenererAffichageMontantOperation(typeOperation, montant)();

            // Mise à jour du solde via le délégué
            miseAJourSolde(montant);

            // Récupérer le nouveau solde via le délégué
            decimal nouveauSolde = obtenirSolde();

            // Enregistrer l'opération dans l'historique sous forme d'une Transaction
            Transaction tx = new Transaction
            {
                Date = DateTime.Now,
                Type = typeOperation,
                Montant = montant,
                NouveauSolde = nouveauSolde
            };
            listeTransactions.Add(tx);
            GenererAffichageSoldeAJour(compte, obtenirSolde)();

            AttendreToucheEntrer();
        }
        #endregion

        #region Affichage des Informations du Titulaire authentifié

        private static void AfficherInformationsTitulaire()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Informations sur le titulaire du compte. ---\n");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Voici vos informations :\n");
            Console.ResetColor();
            Console.WriteLine($"Prénom : {currentUser.Prenom}");
            Console.WriteLine($"Nom : {currentUser.Nom}");
            Console.WriteLine($"Numéro de compte : {currentUser.NumeroCompte}\n\n");
            AttendreToucheEntrer();
        }
        #endregion

        #region Opérations sur les Comptes Bancaires

        // Opérations sur le compte courant
        private static void DeposerCompteCourant()
        {
            Console.Clear();
            TraiterOperation(
                "Dépôt",
                "compte courant",
                "Quel montant souhaitez-vous déposer ? (En chiffres)",
                1000000,
                montant => currentUser.SoldeCompteCourant += montant,
                () => currentUser.SoldeCompteCourant,
                currentUser.TransactionsCompteCourant
            );
        }

        private static void RetirerCompteCourant()
        {
            Console.Clear();
            TraiterOperation(
                "Retrait",
                "compte courant",
                "Quel montant souhaitez-vous retirer ? (En chiffres)",
                currentUser.SoldeCompteCourant,
                montant => currentUser.SoldeCompteCourant -= montant,
                () => currentUser.SoldeCompteCourant,
                currentUser.TransactionsCompteCourant
            );
        }

        // Opérations sur le compte épargne
        private static void DeposerCompteEpargne()
        {
            Console.Clear();
            TraiterOperation(
                "Dépôt",
                "compte épargne",
                "Quel montant souhaitez-vous déposer ? (En chiffres)",
                1000000,
                montant => currentUser.SoldeCompteEpargne += montant,
                () => currentUser.SoldeCompteEpargne,
                currentUser.TransactionsCompteEpargne
            );
        }

        private static void RetirerCompteEpargne()
        {
            Console.Clear();
            TraiterOperation(
                "Retrait",
                "compte épargne",
                "Quel montant souhaitez-vous retirer ? (En chiffres)",
                currentUser.SoldeCompteEpargne,
                montant => currentUser.SoldeCompteEpargne -= montant,
                () => currentUser.SoldeCompteEpargne,
                currentUser.TransactionsCompteEpargne
            );
        }

        // Opérations de virement interne
        private static void VirementInterne()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("--- Virement Interne ---\n");
            Console.ResetColor();

            Console.WriteLine("Choisissez votre virement interne :");
            Console.WriteLine("[1] Virement du compte courant vers le compte épargne");
            Console.WriteLine("[2] Virement du compte épargne vers le compte courant\n");

            string? choix = null;
            do
            {
                SaisieResultat resultatChoix = LireEntreeAvecOptionQuitter("Quel est votre choix : ", "revenir au menu principal.");
                if (resultatChoix.Abandon) return; // Quitte immédiatement
                choix = resultatChoix.Valeur;

                if (choix != "1" && choix != "2")
                {
                    AfficherErreur("Choix invalide. Veuillez réessayer.\n");
                }
            } while (choix != "1" && choix != "2");

            MontantResultat resultatMontant = choix == "1"
                ? ObtenirMontant("\nEntrez le montant à transférer depuis le compte courant : ", currentUser.SoldeCompteCourant)
                : ObtenirMontant("\nEntrez le montant à transférer depuis le compte épargne : ", currentUser.SoldeCompteEpargne);

            if (resultatMontant.Abandon) return; // Quitte immédiatement si abandon
            decimal montant = resultatMontant.Montant!.Value;

            if (choix == "1")
            {
                currentUser.SoldeCompteCourant -= montant;
                currentUser.SoldeCompteEpargne += montant;

                AddTransaction(currentUser.TransactionsCompteCourant, "Virement sortant (vers le compte épargne)", montant, currentUser.SoldeCompteCourant);
                AddTransaction(currentUser.TransactionsCompteEpargne, "Virement entrant (depuis le compte courant)", montant, currentUser.SoldeCompteEpargne);
            }
            else
            {
                currentUser.SoldeCompteEpargne -= montant;
                currentUser.SoldeCompteCourant += montant;

                AddTransaction(currentUser.TransactionsCompteEpargne, "Virement sortant (vers le compte courant)", montant, currentUser.SoldeCompteEpargne);
                AddTransaction(currentUser.TransactionsCompteCourant, "Virement entrant (depuis le compte épargne)", montant, currentUser.SoldeCompteCourant);
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"\nVirement interne de {montant:C} réalisé avec succès !\n");
            Console.ResetColor();
            Console.WriteLine($"Nouveau solde du compte courant : {currentUser.SoldeCompteCourant}");
            Console.WriteLine($"Nouveau solde du compte epargne : {currentUser.SoldeCompteEpargne}\n");
            AttendreToucheEntrer();
        }



        private static void VirementExterne()
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("--- Virement Externe ---\n");
            Console.ResetColor();

            Console.WriteLine("Voulez-vous utiliser un bénéficiaire déjà enregistré ? (O/N)");
            SaisieResultat resultatReponse = LireEntreeAvecOptionQuitter("Votre choix : ", "revenir au menu principal.");

            if (resultatReponse.Abandon) return;

            string reponse = resultatReponse.Valeur!;
            Beneficiaire? beneficiaireSelectionne = null;

            // Gestion des bénéficiaires existants
            if (reponse.Trim().ToUpper() == "O")
            {
                if (currentUser.Beneficiaires.Count == 0)
                {
                    // Si aucun bénéficiaire enregistré, proposer directement d'enregistrer un nouveau bénéficiaire
                    Console.WriteLine("Aucun bénéficiaire enregistré. Voulez-vous enregistrer un nouveau bénéficiaire ? (O/N)");
                    SaisieResultat resultatEnregistrerNouveau = LireEntreeAvecOptionQuitter("Votre choix : ", "revenir au menu principal.");

                    if (resultatEnregistrerNouveau.Abandon) return; // L'utilisateur a choisi de quitter

                    string enregistrerNouveau = resultatEnregistrerNouveau.Valeur!;

                    if (enregistrerNouveau.Trim().ToUpper() == "O")
                    {
                        beneficiaireSelectionne = EnregistrerNouveauBeneficiaire(); // Appel d'une méthode pour enregistrer un nouveau bénéficiaire
                       
                        beneficiaireSelectionne!.Iban = ReformaterIban(beneficiaireSelectionne.Iban);

                        currentUser.Beneficiaires.Add(beneficiaireSelectionne);
                        SauvegarderTitulairesEnJson();
                    }
                    else
                    {
                        AfficherErreur("Aucun bénéficiaire sélectionné. Opération annulée.");
                        return;
                    }
                }
                else
                {
                    do
                    {
                        Console.WriteLine("\nListe des bénéficiaires enregistrés :\n");
                        for (int i = 0; i < currentUser.Beneficiaires.Count; i++)
                        {
                            Console.WriteLine($"[{i}] {currentUser.Beneficiaires[i]}");
                        }

                        SaisieResultat resultat = LireEntreeAvecOptionQuitter("Entrez l'indice du bénéficiaire à utiliser : ", "revenir au menu principal.");
                        if (resultat.Abandon) return; // L'utilisateur a choisi de quitter

                        if (int.TryParse(resultat.Valeur, out int index) && index >= 0 && index < currentUser.Beneficiaires.Count)
                        {
                            beneficiaireSelectionne = currentUser.Beneficiaires[index];
                            break;
                        }

                        AfficherErreur("Indice invalide. Veuillez réessayer.");
                    }
                    while (true);
                }
            }

            // Enregistrement d'un nouveau bénéficiaire si nécessaire
            if (beneficiaireSelectionne == null)
            {
                Console.WriteLine("Voulez-vous enregistrer un nouveau bénéficiaire ? (O/N)");
                SaisieResultat resReponseNouveau = LireEntreeAvecOptionQuitter("Votre choix : ", "revenir au menu principal.");
                if (resReponseNouveau.Abandon) return; // L'utilisateur a choisi de quitter

                string reponseNouveau = resReponseNouveau.Valeur!;

                if (reponseNouveau.Trim().ToUpper() == "O")
                {
                    beneficiaireSelectionne = EnregistrerNouveauBeneficiaire(); // Appel d'une méthode pour enregistrer un nouveau bénéficiaire

                    if (beneficiaireSelectionne != null)
                    {
                        // Ajouter le bénéficiaire à la liste du titulaire actuel
                        beneficiaireSelectionne.Iban = ReformaterIban(beneficiaireSelectionne.Iban); // Formater l'IBAN avant l'enregistrement
                                                
                        currentUser.Beneficiaires.Add(beneficiaireSelectionne);
                        SauvegarderTitulairesEnJson();

                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("Nouveau bénéficiaire enregistré avec succès !");
                        Console.ResetColor();
                    }
                    else
                    {
                        AfficherErreur("Erreur lors de l'enregistrement du bénéficiaire.");
                        return; // Sortir si l'enregistrement échoue
                    }
                }
                else
                {
                    AfficherErreur("Aucun bénéficiaire sélectionné. Opération annulée.");
                    return;
                }
            }

            // Réalisation du virement
            Console.WriteLine($"\nBénéficiaire sélectionné : {beneficiaireSelectionne}");

            // Utilisation de MontantResultat pour gérer la saisie de montant
            MontantResultat resultatMontant = ObtenirMontant("Entrez le montant à transférer : ", currentUser.SoldeCompteCourant);
            if (resultatMontant.Abandon) return; // L'utilisateur a choisi de quitter
            decimal montant = resultatMontant.Montant!.Value;

            currentUser.SoldeCompteCourant -= montant;
            currentUser.TransactionsCompteCourant.Add(new Transaction
            {
                Date = DateTime.Now,
                Type = "Virement externe sortant",
                Montant = montant,
                NouveauSolde = currentUser.SoldeCompteCourant
            });

            if (beneficiaireSelectionne == null)
            {
                AfficherErreur("Aucun bénéficiaire valide n'a été sélectionné. Opération annulée.");
                return;
            }

            Titulaire? benefClient = titulaires.FirstOrDefault(t =>
     NormaliserIbanPourRecherche(t.Iban) == NormaliserIbanPourRecherche(beneficiaireSelectionne!.Iban));

            if (benefClient != null)
            {
                benefClient.SoldeCompteCourant += montant;
                benefClient.TransactionsCompteCourant.Add(new Transaction
                {
                    Date = DateTime.Now,
                    Type = "Virement externe entrant",
                    Montant = montant,
                    NouveauSolde = benefClient.SoldeCompteCourant
                });
            }
            else
            {
                Console.WriteLine("Attention : Le bénéficiaire n'est pas un client enregistré dans le système.");
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("\nVirement externe réalisé avec succès !");
            Console.ResetColor();
            AttendreToucheEntrer();
        }

        #endregion

        #region Quitter l'Application, sauvegarde Json et sauvegarde Incrémentielle (fichiers texte)

        private static void QuitterApplication()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Enregistrement des transactions...");
            Console.ResetColor();

            // Sauvegarder les transactions dans des fichiers texte spécifiques à l'utilisateur avec ref pour la mise à jour de l'indice
            EcrireTransactions("compte_courant", currentUser.TransactionsCompteCourant, ref currentUser.DernierIndiceTransactionsCompteCourant);
            EcrireTransactions("compte_epargne", currentUser.TransactionsCompteEpargne, ref currentUser.DernierIndiceTransactionsCompteEpargne);

            // Sauvegarde finale des données dans le fichier JSON pour persister les mises à jour
            SauvegarderTitulairesEnJson();

            Console.WriteLine("\nMerci d'avoir utilisé l'application. Au revoir !");
            Environment.Exit(0);
        }

        // Écriture les transactions de l'utilisateur dans un fichier texte
        private static void EcrireTransactions(string typeCompte, List<Transaction> transactions, ref int dernierIndice)
        {
            string filePath = "";
            try
            {
                // Générer un nom de fichier unique pour l'utilisateur et le type de compte
                filePath = $"{currentUser.Identifiant}_{currentUser.Nom}_{currentUser.Prenom}_{typeCompte}_transactions.txt";

                // Si toutes les transactions ont déjà été sauvegardées, on ne fait rien.
                if (transactions.Count <= dernierIndice)
                {
                    Console.WriteLine($"Aucune transaction à sauvegarder dans le fichier : {filePath}");
                    return;
                }

                // Ouvre ou crée le fichier correspondant avec append: true pour ajouter et ne pas écraser
                using (StreamWriter sw = new StreamWriter(filePath, append: true))
                {
                    sw.WriteLine($"=== Transactions enregistrées {typeCompte} le {DateTime.Now:G} ===");

                    // Itérer sur les transactions nouvelles (celles dont l'indice est >= dernierIndice)
                    for (int i = dernierIndice; i < transactions.Count; i++)
                    {
                        sw.WriteLine(transactions[i].ToString());
                    }

                    sw.WriteLine("\n");
                }
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"Les transactions ont été sauvegardées dans le fichier : {filePath}");
                Console.ResetColor();

                // Mettre à jour le marqueur pour la prochaine sauvegarde.
                dernierIndice = transactions.Count;
            }

            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"Erreur lors de l'écriture des transactions dans le fichier {filePath} : {ex.Message}");
                Console.ResetColor();
            }
        }
        #endregion
    }

}
