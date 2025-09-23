namespace BlazorSseClient.Demo.Client.SportsScores
{
    public readonly record struct ScoreModel(Guid Id, string Sport, string HomeTeam, string AwayTeam,
        int HomeScore, int AwayScore, string Progress);
}
