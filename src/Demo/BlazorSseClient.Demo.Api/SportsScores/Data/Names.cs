namespace BlazorSseClient.Demo.Api.SportsScores.Data
{
    public static class Names
    {

        private static string[] FirstNames => [
            "Liam", "Noah", "Oliver", "Elijah", "James", "William", "Benjamin", "Lucas", "Henry", "Alexander",
            "Olivia", "Emma", "Ava", "Sophia", "Isabella", "Mia", "Charlotte", "Amelia", "Harper", "Evelyn",
            "Michael", "Ethan", "Daniel", "Matthew", "Aiden", "Joseph", "Jackson", "Samuel", "Sebastian", "David",
            "Carter", "Wyatt", "Jayden", "John", "Owen", "Dylan", "Luke", "Gabriel", "Anthony", "Isaac",
            "Grayson", "Jack", "Julian", "Levi", "Christopher", "Joshua", "Andrew", "Lincoln", "Mateo", "Ryan",
            "Ella", "Scarlett", "Grace", "Chloe", "Victoria", "Riley", "Aria", "Lily", "Aubrey", "Zoey",
            "Penelope", "Lillian", "Addison", "Natalie", "Hannah", "Brooklyn", "Zoe", "Nora", "Leah", "Savannah",
            "Stella", "Hazel", "Ellie", "Paisley", "Audrey", "Skylar", "Violet", "Claire", "Bella", "Aurora",
            "Lucy", "Anna", "Samantha", "Caroline", "Genesis", "Aaliyah", "Kennedy", "Kinsley", "Allison", "Maya",
            "Sarah", "Madelyn", "Adeline", "Alexa", "Ariana", "Elena", "Gabriella", "Naomi", "Alice", "Sadie",
            "Hailey", "Eva", "Emilia", "Autumn", "Quinn", "Nevaeh", "Piper", "Ruby", "Serenity", "Willow",
            "Cory", "Kayla", "Sophie", "Lydia", "Ariana", "Clara", "Vivian", "Madeline", "Peyton", "Julia",
            "Everly", "Cora", "Kaylee", "Lydia", "Aubree", "Madison", "Josephine", "Delilah", "Peyton", "Clara",
            "Vivian", "Rylee", "Liliana", "Sophie", "Brielle", "Madeline", "Parker", "Julia", "Katherine", "Jade",
            "Mackenzie", "Valentina", "Isabelle", "Reagan", "Ximena", "Hadley", "Melanie", "Gianna", "Isabel", "Natalia",
            "Raelynn", "Eliana", "Luna", "Alyssa", "Cecilia", "Adalynn", "Arya", "Margaret", "Lyla", "Athena",
            "Ryleigh", "Melody", "Maria", "Amaya", "Alina", "Isla", "Rose", "Emery", "Elliana", "Leilani",
            "Molly", "Kylie", "Brooke", "Jasmine", "Adriana", "Xavier", "Jaxon", "Leonardo", "Josiah", "Hudson",
            "Lincoln", "Ezra", "Anthony", "Charles", "Thomas", "Caleb", "Christopher", "Josiah", "Eli", "Miles",
            "CJ", "TJ", "Max", "Leo", "Axel", "Bentley", "Micah", "Rowan", "Sawyer", "Weston", "Declan",
            "Silas", "Nathan", "Ryder", "Damian", "Everett", "Emmett", "Micah", "Colton", "Luca", "Richard",
            "Asher", "Jace", "Brayden", "Gael", "Diego", "Vincent", "Kayden", "Bryson", "Harrison", "Kingston",
            "Jason", "Maxwell", "Juan", "Ivan", "Maverick", "Justin", "Brandon", "Adam", "Jude", "Xander",
            "Kevin", "Elias", "Ezekiel", "Carlos", "Matteo", "Emiliano", "Malachi", "Jasper", "Gavin", "Nolan",
            "Genevieve", "Ariana", "Eliza", "Catherine", "Vivienne", "Claudia", "Sienna", "Adelyn", "Alaina", "Lilah",
            "Janice", "Rosemary", "Daisy", "Summer", "Lena", "Melina", "Alayna", "Mya", "Margot", "Catalina",
            "Jane", "Amara", "Sloane", "Juliana", "Kaitlyn", "Alani", "Ruth", "Mckenna", "Diana", "Brianna",
            "Jocelyn", "Adelynn", "Lia", "Presley", "Maggie", "Holly", "Cali", "Tessa", "Blakely", "Anastasia",
            "Elizabeth", "Athena", "Lilliana", "Callie", "Luna", "Rosalie", "Alivia", "Emersyn", "Malia", "Kendra",
            "Lynn", "Mckenna", "Cameron", "Sage", "Alessandra", "Julianna", "Miranda", "Raegan", "Daphne", "Miriam",
            "Mackenzie", "Arielle", "Kali", "Maddison", "Lena", "Francesca", "Cynthia", "Zariah", "Annie", "Mckenzie",
            "Tabitha", "Lilian", "Heidi", "Makenna", "Kelsey", "Allyson", "Caitlyn", "Jenna", "Kara", "Macy",
            "Kelly", "Rachael", "Caitlin", "Jillian", "Cassidy", "Angelica", "Tatum", "Lorelai", "Mollie", "Kiera"
        ];

        private static string[] LastNames => [
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
            "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
            "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson",
            "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
            "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell", "Carter", "Roberts",
            "Gomez", "Phillips", "Evans", "Turner", "Diaz", "Parker", "Cruz", "Edwards", "Collins", "Reyes",
            "Stewart", "Morris", "Morales", "Murphy", "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper",
            "Peterson", "Bailey", "Reed", "Kelly", "Maass", "Howard", "Ramos", "Kim", "Cox", "Ward", "Richardson",
            "Watson", "Brooks", "Chavez", "Wood", "James", "Bennett", "Gray", "Mendoza", "Ruiz", "Hughes",
            "Price", "Alvarez", "Castillo", "Sanders", "Patel", "Myers", "Long", "Ross", "Foster", "Jimenez",
            "Powell", "Jenkins", "Perry", "Russell", "Sullivan", "Bell", "Coleman", "Butler", "Henderson", "Barnes",
            "Fisher", "Vasquez", "Simmons", "Romero", "Jordan", "Patterson", "Alexander", "Hamilton", "Graham", "Reynolds",
            "Griffin", "Wallace", "Moreno", "West", "Cole", "Hayes", "Bryant", "Herrera", "Gibson", "Ellis",
            "Tran", "Medina", "Aguilar", "Stevens", "Murray", "Ford", "Castro", "Marshall", "Owens", "Harrison",
            "Fernandez", "Mcdonald", "Woods", "Washington", "Kennedy", "Wells", "Vargas", "Henry", "Chen", "Freeman",
            "Calton", "Mendez", "Webb", "Simpson", "Steele", "Tucker", "Porter", "Hunter", "Hicks", "Crawford",
            "Romanov", "Hanson", "Johnston", "Clayton", "Montgomery", "Richards", "Williamson", "Johns", "Fletcher", "Moran",
            "Reece", "Sampson", "Mcclain", "Lamb", "Puckett", "Shepherd", "Clements", "Barrett", "Booth", "Levine",
            "Hall", "Grant", "Knight", "Hudson", "Riley", "Carroll", "Duncan", "Armstrong", "Berry", "Andrews", "Johnston"
        ];

        public static string GetRandomFullName()
        {
            var firstName = FirstNames[Random.Shared.Next(FirstNames.Length)];
            var lastName = LastNames[Random.Shared.Next(LastNames.Length)];

            while (firstName == lastName)
                lastName = LastNames[Random.Shared.Next(LastNames.Length)];

            return $"{firstName} {lastName}";
        }

        public static (string, string) GetRandomMatchup()
        {
            var name1 = GetRandomFullName();
            var name2 = GetRandomFullName();

            while (name1 == name2)
                name2 = GetRandomFullName();
            
            return (name1, name2);
        }
    }
}
