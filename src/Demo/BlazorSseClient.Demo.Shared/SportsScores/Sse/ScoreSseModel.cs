namespace BlazorSseClient.Demo.Shared.SportsScores.Sse
{
    public readonly record struct ScoreSseModel(Guid Id, string Sport, string HomeTeam, string AwayTeam, 
        int HomeScore, int AwayScore, string Progress);

}
