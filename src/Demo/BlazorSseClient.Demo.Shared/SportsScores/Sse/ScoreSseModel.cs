namespace BlazorSseClient.Demo.Shared.SportsScores.Sse
{
    public readonly record struct ScoreSseModel(string Sport, string HomeTeam, string AwayTeam, 
        int HomeScore, int AwayScore, string Progress);

}
