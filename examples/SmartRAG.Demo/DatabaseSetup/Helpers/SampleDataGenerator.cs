
namespace SmartRAG.Demo.DatabaseSetup.Helpers;

/// <summary>
/// Helper class to generate diverse international sample data
/// </summary>
public static class SampleDataGenerator
{
    #region European Names Data

    private static readonly string[] EuropeanFirstNames = new[]
    {
        // German
        "Hans", "Franz", "Klaus", "Werner", "Helmut", "Dieter", "Jürgen", "Peter",
        "Anna", "Maria", "Petra", "Monika", "Sabine", "Heike", "Karin", "Ute",
        
        // French
        "Jean", "Pierre", "Michel", "Philippe", "Jacques", "Bernard", "André", "François",
        "Marie", "Sophie", "Isabelle", "Nathalie", "Catherine", "Sylvie", "Monique", "Françoise",
        
        // Italian
        "Marco", "Andrea", "Giovanni", "Giuseppe", "Antonio", "Francesco", "Alessandro", "Paolo",
        "Laura", "Francesca", "Chiara", "Sara", "Valentina", "Elena", "Anna", "Giulia",
        
        // Spanish
        "José", "Carlos", "Juan", "Antonio", "Manuel", "Francisco", "Luis", "Miguel",
        "María", "Carmen", "Ana", "Isabel", "Dolores", "Pilar", "Teresa", "Rosa",
        
        // English
        "James", "John", "Robert", "Michael", "William", "David", "Richard", "Thomas",
        "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth", "Barbara", "Susan", "Jessica",
        
        // Dutch
        "Jan", "Pieter", "Kees", "Henk", "Willem", "Dirk", "Erik", "Maarten",
        "Anna", "Emma", "Sophie", "Lisa", "Eva", "Anne", "Sara", "Julia",
        
        // Swedish
        "Lars", "Anders", "Johan", "Erik", "Karl", "Per", "Mikael", "Magnus",
        "Anna", "Maria", "Eva", "Karin", "Sara", "Ingrid", "Birgitta", "Lena",
        
        // Polish
        "Jan", "Piotr", "Andrzej", "Krzysztof", "Stanisław", "Tomasz", "Paweł", "Józef",
        "Anna", "Maria", "Katarzyna", "Małgorzata", "Agnieszka", "Barbara", "Ewa", "Elżbieta",
        
        // Turkish (some)
        "Ahmet", "Mehmet", "Ali", "Mustafa", "Ayşe", "Fatma", "Zeynep", "Elif"
    };

    private static readonly string[] EuropeanLastNames = new[]
    {
        // German
        "Müller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer", "Wagner", "Becker",
        "Schulz", "Hoffmann", "Schäfer", "Koch", "Bauer", "Richter", "Klein", "Wolf",
        
        // French
        "Martin", "Bernard", "Dubois", "Thomas", "Robert", "Petit", "Durand", "Leroy",
        "Moreau", "Simon", "Laurent", "Lefebvre", "Michel", "Garcia", "David", "Bertrand",
        
        // Italian
        "Rossi", "Russo", "Ferrari", "Esposito", "Bianchi", "Romano", "Colombo", "Ricci",
        "Marino", "Greco", "Bruno", "Gallo", "Conti", "De Luca", "Costa", "Giordano",
        
        // Spanish
        "García", "Rodríguez", "González", "Fernández", "López", "Martínez", "Sánchez", "Pérez",
        "Martín", "Gómez", "Ruiz", "Díaz", "Hernández", "Moreno", "Jiménez", "Álvarez",
        
        // English
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
        "Wilson", "Moore", "Taylor", "Anderson", "Thomas", "Jackson", "White", "Harris",
        
        // Dutch
        "De Jong", "Jansen", "De Vries", "Van den Berg", "Van Dijk", "Bakker", "Visser", "Smit",
        "Meijer", "De Boer", "Mulder", "De Groot", "Bos", "Vos", "Peters", "Hendriks",
        
        // Swedish
        "Johansson", "Andersson", "Karlsson", "Nilsson", "Eriksson", "Larsson", "Olsson", "Persson",
        "Svensson", "Gustafsson", "Pettersson", "Jonsson", "Jansson", "Hansson", "Bengtsson", "Lindberg",
        
        // Polish
        "Nowak", "Kowalski", "Wiśniewski", "Wójcik", "Kamiński", "Lewandowski", "Zieliński", "Szymański",
        "Woźniak", "Dąbrowski", "Kozłowski", "Jankowski", "Mazur", "Kwiatkowski", "Krawczyk", "Piotrowski",
        
        // Turkish (some)
        "Yılmaz", "Demir", "Kaya", "Çelik", "Şahin", "Özkan", "Aydın", "Türk"
    };

    private static readonly string[] EuropeanCities = new[]
    {
        "Berlin", "Munich", "Hamburg", "Cologne", "Frankfurt", // Germany
        "Paris", "Lyon", "Marseille", "Toulouse", "Nice", // France
        "Rome", "Milan", "Naples", "Turin", "Florence", // Italy
        "Madrid", "Barcelona", "Valencia", "Seville", "Bilbao", // Spain
        "London", "Manchester", "Birmingham", "Liverpool", "Leeds", // UK
        "Amsterdam", "Rotterdam", "The Hague", "Utrecht", "Eindhoven", // Netherlands
        "Stockholm", "Gothenburg", "Malmö", "Uppsala", "Västerås", // Sweden
        "Warsaw", "Kraków", "Łódź", "Wrocław", "Poznań", // Poland
        "Vienna", "Graz", "Linz", "Salzburg", "Innsbruck", // Austria
        "Brussels", "Antwerp", "Ghent", "Charleroi", "Liège", // Belgium
        "Istanbul", "Ankara", "Izmir", "Bursa", "Antalya" // Turkey
    };

    private static readonly string[] Countries = new[]
    {
        "Germany", "France", "Italy", "Spain", "United Kingdom",
        "Netherlands", "Sweden", "Poland", "Austria", "Belgium",
        "Denmark", "Finland", "Norway", "Portugal", "Greece",
        "Czech Republic", "Hungary", "Romania", "Ireland", "Turkey"
    };

    #endregion

    #region Helper Methods

    public static string GetRandomFirstName(Random random)
    {
        return EuropeanFirstNames[random.Next(EuropeanFirstNames.Length)];
    }

    public static string GetRandomLastName(Random random)
    {
        return EuropeanLastNames[random.Next(EuropeanLastNames.Length)];
    }

    public static string GetRandomCity(Random random)
    {
        return EuropeanCities[random.Next(EuropeanCities.Length)];
    }

    public static string GetRandomCountry(Random random)
    {
        return Countries[random.Next(Countries.Length)];
    }

    public static string GenerateEmail(string firstName, string lastName, Random random)
    {
        var domains = new[] { "email.com", "example.com", "mail.com", "company.com", "business.com" };
        var domain = domains[random.Next(domains.Length)];
        
        return $"{firstName.ToLower()}.{lastName.ToLower().Replace(" ", "")}@{domain}";
    }

    public static string GeneratePhone(Random random)
    {
        var countryCodes = new[] { "+49", "+33", "+39", "+34", "+44", "+31", "+46", "+48", "+90" };
        var countryCode = countryCodes[random.Next(countryCodes.Length)];
        
        return $"{countryCode} {random.Next(100, 999)} {random.Next(1000000, 9999999)}";
    }

    public static string GenerateAddress(Random random)
    {
        var streets = new[] 
        { 
            "Main Street", "High Street", "Park Avenue", "Church Road", "Station Road",
            "Green Lane", "Manor Drive", "School Street", "Victoria Road", "Albert Street",
            "Castle Street", "Mill Lane", "Broadway", "Oak Avenue", "Elm Street"
        };
        
        var street = streets[random.Next(streets.Length)];
        var number = random.Next(1, 999);
        
        return $"{number} {street}";
    }

    #endregion
}

