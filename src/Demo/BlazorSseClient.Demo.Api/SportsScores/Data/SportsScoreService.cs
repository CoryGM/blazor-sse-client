namespace BlazorSseClient.Demo.Api.SportsScores.Data
{
    public class SportsScoreService : ISportsScoreService
    {
        private readonly string[] _sports = ["Basketball", "Baseball", "Hockey", "Tennis", "Football", "Soccer"];

        public Score GetScoreForSport(string sport)
        {
            string homeTeam;
            string awayTeam;

            if (!_sports.Contains(sport, StringComparer.OrdinalIgnoreCase))
                sport = "Football";



            if (sport.Equals("Tennis", StringComparison.OrdinalIgnoreCase))
                (homeTeam, awayTeam) = Names.GetRandomMatchup();
            else
                (homeTeam, awayTeam) = Teams.GetRandomMatchup();

            var homeScore = GetScore(sport);
            var awayScore = GetScore(sport);
            var progress = GetProgress(sport);

            return new Score(Guid.CreateVersion7(), sport, homeTeam, awayTeam, homeScore, awayScore, progress);
        }

        public Score GetRandomScore()
        {
            var sport = GetRandomSport();

            return GetScoreForSport(sport);
        }

        public string[] Sports => [.. _sports];

        private static string GetProgress(string sport)
        {
            var isFinal = Random.Shared.Next(0, 10) > 8;

            if (isFinal)
                return "Final";

            return sport switch
            {
                "Basketball" => $"{GetWithNumericSuffix(Random.Shared.Next(1, 4))} Qtr",
                "Baseball" => $"{GetWithNumericSuffix(Random.Shared.Next(1, 9))} Inn",
                "Hockey" => $"{GetWithNumericSuffix(Random.Shared.Next(1, 3))} period",
                "Tennis" => $"{GetWithNumericSuffix(Random.Shared.Next(1, 5))} Set",
                "Football" => $"{GetWithNumericSuffix(Random.Shared.Next(1, 4))} Qtr",
                "Soccer" => $"{Random.Shared.Next(1, 90)}'",
                _ => "In Progress"
            };
        }

        private static string GetWithNumericSuffix(int number)
        {
            return number switch
            {
                1 => "1st",
                2 => "2nd",
                3 => "3rd",
                _ => $"{number}th"
            };
        }

        private static int GetScore(string sport)
        {
            return sport switch
            {
                "Basketball" => Random.Shared.Next(40, 130),
                "Baseball" => Random.Shared.Next(0, 12),
                "Hockey" => Random.Shared.Next(0, 7),
                "Tennis" => Random.Shared.Next(0, 3),
                "Football" => Random.Shared.Next(0, 60),
                "Soccer" => Random.Shared.Next(0, 5),
                _ => 0
            };
        }

        private string GetRandomSport()
        {
            var index = Random.Shared.Next(0, _sports.Length);

            return _sports[index];
        }
    }
}
