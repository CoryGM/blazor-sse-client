using System.Collections.Concurrent;

namespace BlazorSseClient.Demo.Api.SportsScores.Data
{
    public static class Teams
    {
        private readonly static ConcurrentQueue<string> _teamNamesUsed = [];

        private readonly static List<string> teamNames =
        [
            "Raptors", "Bears", "Gators", "Cowboys", "Panthers", "Patriots", "Sharks",
            "Griffins", "Antelopes", "Pythons", "Narwhals", "Wolves", "Spartans", "Tigers",
            "Eagles", "Hawks", "Lions", "Dragons", "Cheetahs", "Rhinos", "Bulls", "Cougars",
            "Dolphins", "Vipers", "Pioneers", "Zebras", "Hornets", "Mustangs", "Cobras", "Pipers",
            "Falcons", "Titans", "Warriors", "Knights", "Raiders", "Giants", "Cyclones", "Ravens",
            "Scorpions", "Warthogs", "Barracudas", "Leopards", "Cranes", "Stallions", "Mavericks",
            "Phoenix", "Comets", "Gladiators", "Jets", "Blazers", "Thunder", "Avalanche", "Vikings",
            "Packers", "Cavaliers", "Rockets", "Suns", "Pelicans", "Timberwolves", "Jazz", "Clippers",
            "Nuggets", "Wizards", "Heat", "Celtics", "Bucks", "76ers", "Nets", "Knicks", "Spurs",
            "Grizzlies", "Kings", "Thunder", "Rockets", "Jazz", "Clippers", "Suns"
        ];

        /// <summary>
        /// Returns a random matchup of two different teams.
        /// </summary>
        /// <returns></returns>
        public static (string team1, string team2) GetRandomMatchup()
        {
            var teamName1 = GetRandomTeamName();
            var teamName2 = GetRandomTeamName();    

            return (teamName1, teamName2);
        }

        private static string GetRandomTeamName()
        {
            var teamName = teamNames[Random.Shared.Next(teamNames.Count)];

            while (_teamNamesUsed.Contains(teamName))
                teamName = teamNames[Random.Shared.Next(teamNames.Count)];

            _teamNamesUsed.Enqueue(teamName);

            if (_teamNamesUsed.Count > 12) {    
                _teamNamesUsed.TryDequeue(out _);
            }

            return teamName;
        }
    }
}
