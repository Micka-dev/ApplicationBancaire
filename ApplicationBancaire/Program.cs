namespace MonApplicationCompteBancaire;

using System;
using System.Globalization;

public class MesComptesBancaire
{
    private static decimal soldeCompteCourant = 0;
    private static decimal soldeCompteEpargne = 0;

    private static List<string> transactionsCompteCourant = new List<string>();
    private static List<string> transactionsCompteEpargne = new List<string>();


    public static decimal SoldeCompteCourant { get => soldeCompteCourant; set => soldeCompteCourant = value; }
    public static decimal SoldeCompteEpargne { get => soldeCompteEpargne; set => soldeCompteEpargne = value; }

    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
        System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");

        ChargerSoldesDepuisJson();
        AttendreToucheEntrer();
        LancerMenuPrincipal();
    }


    public class Soldes
    {
        public decimal CompteCourant { get; set; }
        public decimal CompteEpargne { get; set; }
    }

    private static void ChargerSoldesDepuisJson()
    {
        string filePath = "soldes.json";
        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                Soldes soldes = System.Text.Json.JsonSerializer.Deserialize<Soldes>(json);
                SoldeCompteCourant = soldes.CompteCourant;
                SoldeCompteEpargne = soldes.CompteEpargne;
                Console.WriteLine("Les soldes ont été chargés depuis le fichier JSON.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Aucun fichier JSON de sauvegarde trouvé. Les soldes par défaut seront utilisés.");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Erreur lors du chargement des soldes : {ex.Message}");
            Console.WriteLine("Les soldes par défaut seront utilisés.");
            Console.ResetColor();
        }
    }

    private static void SauvegarderSoldesEnJson()
    {
        string filePath = "soldes.json";
        Soldes soldes = new Soldes
        {
            CompteCourant = SoldeCompteCourant,
            CompteEpargne = SoldeCompteEpargne
        };

        string json = System.Text.Json.JsonSerializer.Serialize(soldes);
        File.WriteAllText(filePath, json);

        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine("Les soldes ont été sauvegardés en JSON.");
        Console.ResetColor();
    }


    private static void AttendreToucheEntrer()
    {
        Console.WriteLine("Appuyez sur 'Entrée' pour accéder au menu");
        while (Console.ReadKey(intercept: true).Key != ConsoleKey.Enter)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("\nCe n'est pas la touche Entrée, réessayez !");
            Console.ResetColor();
        }
    }

    private static void LancerMenuPrincipal()
    {
        Dictionary<string, Action> options = new()
        {
            { "I", AfficherInformationsTitulaire },
            { "CS", () => ConsulterSolde("Compte courant", SoldeCompteCourant) },
            { "CD", DeposerCompteCourant },
            { "CR", RetirerCompteCourant },
            { "ES", () => ConsulterSolde("Compte épargne", SoldeCompteEpargne) },
            { "ED", DeposerCompteEpargne },
            { "ER", RetirerCompteEpargne },
            { "X", QuitterApplication }
        };

        while (true)
        {
            AfficherMenu();
            Console.Write("\nEntrez votre choix : ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Aucune entrée détectée. Veuillez réessayer.");
                Console.ResetColor();
                AttendreToucheEntrer();

                continue;
            }

            string choix = input.ToUpper();
            if (options.ContainsKey(choix))
            {
                options[choix](); // Exécute l'action
                if (choix == "X")
                {
                    break; // Sortir si l'utilisateur choisit de quitter
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Option invalide. Veuillez réessayer. ");
                Console.ResetColor();
                AttendreToucheEntrer();

            }
        }
    }

    private static void AfficherMenu()
    {
        Console.Clear(); // Efface l'écran pour un affichage plus propre

        // Mise en forme et affichage coloré pour plus de lisibilité
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================== Vous êtes sur le menu de votre application bancaire ! ========================\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Veuillez entrer le code correspondant au service et valider avec 'Entrée' pour y accéder.");
        Console.WriteLine("(Par exemple, taper 'I' et appuyer sur 'Entrée' pour accéder à vos informations)\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("----- Informations sur le titulaire du compte ----");
        Console.ResetColor();
        Console.WriteLine("[ I ] Consulter vos informations.\n");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("----------------- Compte Courant -----------------");
        Console.ResetColor();
        Console.WriteLine("[ CS ] Consulter le solde de votre compte courant.");
        Console.WriteLine("[ CD ] Déposer des fonds sur votre compte courant.");
        Console.WriteLine("[ CR ] Retirer des fonds sur votre compte courant. \n");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("----------------- Compte Epargne -----------------");
        Console.ResetColor();
        Console.WriteLine("[ ES ] Consulter le solde de votre compte épargne.");
        Console.WriteLine("[ ED ] Déposer des fonds sur votre compte épargne.");
        Console.WriteLine("[ ER ] Retirer des fonds sur votre compte épargne.\n");

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("------------- Sortir de l'application ------------");
        Console.WriteLine("[ X ] Quitter l'application.\n");
        Console.ResetColor();
    }

    private static void ConsulterSolde(string compte, decimal solde)
    {
        // Méthode commune pour consulter les soldes
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n--- Consultation du solde du {compte}. ---\n");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($"Le solde de votre {compte.ToLower()} est de {solde:C}.\n");
        Console.ResetColor();

        AttendreToucheEntrer();
    }


    private static decimal ConvertirMontant(string input)
    {
        if (decimal.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out decimal nombre) ||
            decimal.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out nombre))
        {
            return nombre;
        }
        return -1; // Valeur indicatrice d'une conversion ratée
    }

    // Méthode générique pour lire et valider un montant
    private static decimal ObtenirMontant(string message, decimal maxValue)
    {
        decimal montant = 0;
        bool montantValide = false;

        while (!montantValide)
        {
            Console.WriteLine(message);
            string input = Console.ReadLine();
            montant = ConvertirMontant(input);

            if (montant > 0 && montant <= maxValue)
            {
                montantValide = true;
            }
            else if (montant > maxValue)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"Veuillez entrer un montant inférieur ou égal à {maxValue:C}.\n");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Entrée invalide. Veuillez réessayer.\n");
                Console.ResetColor();
            }
        }

        return montant;
    }

    private static void EcrireTransactions(string filePath, List<string> transactions)
    {
        try
        {
            // Vérifie si la liste est vide
            if (transactions.Count == 0)
            {
                Console.WriteLine($"Aucune transaction à sauvegarder dans le fichier : {filePath}");
                return; // Ne fait rien si la liste est vide
            }

            // Ouvre le fichier en mode ajout
            using (StreamWriter sw = new StreamWriter(filePath, append: true))
            {
                // Horodatage 
                sw.WriteLine($"=== Transactions enregistrées le {DateTime.Now:G} ===");

                foreach (string transaction in transactions)
                {
                    sw.WriteLine(transaction);
                }

                sw.WriteLine("\n"); 
            }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Les transactions ont été sauvegardées dans le fichier : {filePath}");
            Console.ResetColor();

            // Vider la liste après sauvegarde pour éviter les doublons et économiser de la mémoire
            transactions.Clear();

        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Erreur lors de l'écriture des transactions dans le fichier {filePath} : " + ex.Message);
            Console.ResetColor();
        }
    }

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

    private static void TraiterOperation(
    string typeOperation,
    string compte,
    string message,
    decimal maxValue,
    Action<decimal> miseAJourSolde,
    Func<decimal> obtenirSolde,
    List<string> listeTransactions
)
    {
        GenererAffichageTitreTransaction(compte, typeOperation)();

        // Lecture et validation du montant
        decimal montant = ObtenirMontant(message, maxValue);

        GenererAffichageMontantOperation(typeOperation, montant)();

        // Mise à jour du solde via le délégué
        miseAJourSolde(montant);

        // Récupérer le nouveau solde via le délégué
        decimal nouveauSolde = obtenirSolde();

        // Enregistrer dans l'historique
        string signe = typeOperation == "Dépôt" ? "+" : "-";
        listeTransactions.Add($"[{typeOperation}] {DateTime.Now:G} : {signe}{montant:C}. Nouveau solde : {nouveauSolde:C}");

        GenererAffichageSoldeAJour(compte, obtenirSolde)();

        AttendreToucheEntrer();
    }


    // Méthodes d'action
    private static void AfficherInformationsTitulaire()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n--- Informations sur le titulaire du compte. ---\n");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Voici vos informations :\n");
        Console.ResetColor();
        Console.WriteLine("Prénom : John\n");
        Console.WriteLine("Nom : Doe\n");
        Console.WriteLine("Numéro de compte : xxxx xxxx xxxx\n\n");

        AttendreToucheEntrer();
    }

    private static void DeposerCompteCourant()
    {
        TraiterOperation(
            "Dépôt",
            "compte courant",
            "Quel montant souhaitez-vous déposer ? (En chiffres)",
            1000000,
            montant => SoldeCompteCourant += montant,
            () => SoldeCompteCourant,
            transactionsCompteCourant
        );
    }

    private static void RetirerCompteCourant()
    {
        TraiterOperation(
            "Retrait",
            "compte courant",
            "Quel montant souhaitez-vous retirer ? (En chiffres)",
            SoldeCompteCourant,
            montant => SoldeCompteCourant -= montant,
            () => SoldeCompteCourant,
            transactionsCompteCourant
        );
    }

    private static void DeposerCompteEpargne()
    {
        TraiterOperation(
            "Dépôt",
            "compte épargne",
            "Quel montant souhaitez-vous déposer ? (En chiffres)",
            1000000,
            montant => SoldeCompteEpargne += montant,
            () => SoldeCompteEpargne,
            transactionsCompteEpargne
        );
    }

    private static void RetirerCompteEpargne()
    {
        TraiterOperation(
            "Retrait",
            "compte épargne",
            "Quel montant souhaitez-vous retirer ? (En chiffres)",
            SoldeCompteEpargne,
            montant => SoldeCompteEpargne -= montant,
            () => SoldeCompteEpargne,
            transactionsCompteEpargne
        );
    }

    private static void QuitterApplication()
    {
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine("Enregistrement des transactions...");
        Console.ResetColor();
        Console.WriteLine("\nMerci d'avoir utilisé l'application. Au revoir !");

        SauvegarderSoldesEnJson();
        EcrireTransactions("compte_courant.txt", transactionsCompteCourant);
        EcrireTransactions("compte_epargne.txt", transactionsCompteEpargne);
        Console.WriteLine("\n\n");
        Environment.Exit(0);
    }
}