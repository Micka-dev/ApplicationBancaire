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
            { "I", VoirInformationsTitulaire },
            { "CS", ConsulterCompteCourant },
            { "CD", DeposerCompteCourant },
            { "CR", RetirerCompteCourant },
            { "ES", ConsulterCompteEpargne },
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

        // Afficher un titre coloré
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================== Vous êtes sur le menu de votre application bancaire ! ========================\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Veuillez entrer le code correspondant au service et valider avec 'Entrée' pour y accéder.");
        Console.WriteLine("(Par exemple, taper 'I' et appuyer sur 'Entrée' pour accéder à vos informations)\n");
        Console.ResetColor();

        // Section Informations titulaire (en jaune par exemple)
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("----- Informations sur le titulaire du compte ----");
        Console.ResetColor();
        Console.WriteLine("[ I ] Consulter vos informations.\n");

        // Compte Courant
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("----------------- Compte Courant -----------------");
        Console.ResetColor();
        Console.WriteLine("[ CS ] Consulter le solde de votre compte courant.");
        Console.WriteLine("[ CD ] Déposer des fonds sur votre compte courant.");
        Console.WriteLine("[ CR ] Retirer des fonds sur votre compte courant. \n");

        // Compte Epargne
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("----------------- Compte Epargne -----------------");
        Console.ResetColor();
        Console.WriteLine("[ ES ] Consulter le solde de votre compte épargne.");
        Console.WriteLine("[ ED ] Déposer des fonds sur votre compte épargne.");
        Console.WriteLine("[ ER ] Retirer des fonds sur votre compte épargne.\n");

        // Option de sortie en rouge
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("------------- Sortir de l'application ------------");
        Console.WriteLine("[ X ] Quitter l'application.\n");
        Console.ResetColor();
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
                // Écrit l'horodatage uniquement si des transactions existent
                sw.WriteLine($"=== Transactions enregistrées le {DateTime.Now:G} ===");

                // Écrit chaque transaction dans le fichier
                foreach (string transaction in transactions)
                {
                    sw.WriteLine(transaction);
                }

                sw.WriteLine("\n"); // Ajoute un saut de ligne pour séparer les sessions
            }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Les transactions ont été sauvegardées dans le fichier : {filePath}");
            Console.ResetColor();

            // Vider la liste après sauvegarde
            transactions.Clear();

        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Erreur lors de l'écriture des transactions dans le fichier {filePath} : " + ex.Message);
            Console.ResetColor();
        }
    }

    // Méthodes d'action
    private static void VoirInformationsTitulaire()
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
    private static void ConsulterCompteCourant()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n--- Consultation du solde du compte courant. ---\n");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($"Le solde de votre compte courant est de {SoldeCompteCourant:C}. \n\n");
        Console.ResetColor();

        AttendreToucheEntrer();
    }

    private static void DeposerCompteCourant()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n--- Déposer des fonds sur le compte courant. ---\n");
        Console.ResetColor();

        decimal montantChoisi = ObtenirMontant("Quel montant souhaitez-vous déposer ? (En chiffres)", 1000000);

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"La somme déposée est de: {montantChoisi:C}");
        Console.ResetColor();

        SoldeCompteCourant += montantChoisi;

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($"Votre nouveau solde est de : {SoldeCompteCourant:C}\n");
        Console.ResetColor();

        // Ajout de la transaction dans l'historique
        transactionsCompteCourant.Add(
            $"[Dépôt] {DateTime.Now:G} : +{montantChoisi:C}. Nouveau solde : {SoldeCompteCourant:C}"
        );

        AttendreToucheEntrer();
    }

    private static void RetirerCompteCourant()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n--- Retirer des fonds du compte courant. ---\n");
        Console.ResetColor();

        decimal montantChoisi = ObtenirMontant("Quel montant souhaitez-vous retirer ? (En chiffres)", SoldeCompteCourant);

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"La somme retirée est de : {montantChoisi:C}");
        Console.ResetColor();

        SoldeCompteCourant -= montantChoisi;

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($"Votre nouveau solde est de : {SoldeCompteCourant:C}\n");
        Console.ResetColor();

        // Ajout de la transaction dans l'historique
        transactionsCompteCourant.Add(
            $"[Retrait] {DateTime.Now:G} : -{montantChoisi:C}. Nouveau solde : {SoldeCompteCourant:C}"
        );

        AttendreToucheEntrer();
    }

    private static void ConsulterCompteEpargne()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n--- Consultation du solde du compte épargne. ---\n");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($"Le solde de votre compte épargne est de {SoldeCompteEpargne:C}.\n\n");
        Console.ResetColor();

        AttendreToucheEntrer();
    }


    private static void DeposerCompteEpargne()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n--- Déposer des fonds sur le compte épargne. ---\n");
        Console.ResetColor();

        decimal montantChoisi = ObtenirMontant("Quel montant souhaitez-vous déposer ? (En chiffres)", 1000000);

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"La somme déposée est de : {montantChoisi:C}");
        Console.ResetColor();

        SoldeCompteEpargne += montantChoisi;

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($"Votre nouveau solde est de : {SoldeCompteEpargne:C}\n");
        Console.ResetColor();
        // Ajout de la transaction dans l'historique
        transactionsCompteEpargne.Add(
            $"[Dépôt] {DateTime.Now:G} : +{montantChoisi:C}. Nouveau solde : {soldeCompteEpargne:C}"
        );


        AttendreToucheEntrer();
    }

    private static void RetirerCompteEpargne()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n--- Retirer des fonds du compte épargne. ---\n");
        Console.ResetColor();

        decimal montantChoisi = ObtenirMontant("Quel montant souhaitez-vous retirer ? (En chiffres)", SoldeCompteEpargne);

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"La somme retirée est de : {montantChoisi:C}");
        Console.ResetColor();

        SoldeCompteEpargne -= montantChoisi;

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($"Votre nouveau solde est de : {SoldeCompteEpargne:C}\n");
        Console.ResetColor();
        // Ajout de la transaction dans l'historique
        transactionsCompteEpargne.Add(
            $"[Retrait] {DateTime.Now:G} : -{montantChoisi:C}. Nouveau solde : {soldeCompteEpargne:C}"
        );

        AttendreToucheEntrer();
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