namespace BlazorSseClient.Demo.Api.SportsScores.Data
{
    public static class Teams
    {
        private static List<string> teamNames =
        [
            "Raptors",
            "Bears",
            "Gators",
            "Cowboys",
            "Panthers",
            "Patriots",
            "Sharks",
            "Griffins",
            "Antelopes",
            "Pythons",
            "Narwhals",
            "Wolves",
            "Spartans",
            "Tigers",
            "Eagles",
            "Hawks",
            "Lions",
            "Dragons",
            "Cheetahs",
            "Falcons",
            "Rhinos",
            "Bulls",
            "Cougars",
            "Dolphins",
            "Vipers",
            "Pioneers",
            "Zebras",
            "Hornets",
            "Mustangs",
            "Cobras",
            "Pipers"
        ];

        /// <summary>
        /// Returns a random matchup of two different teams.
        /// </summary>
        /// <returns></returns>
        public static (string team1, string team2) GetRandomMatchup()
        {
            var rnd = new Random();
            int index1 = rnd.Next(teamNames.Count);
            int index2;
         
            do
            {
                index2 = rnd.Next(teamNames.Count);
            } while (index2 == index1);

            return (teamNames[index1], teamNames[index2]);
        }
    }
}
