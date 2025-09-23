namespace BlazorSseClient.Demo.Api.SportsScores.Data
{
    public readonly record struct Score(Guid Id, string Sport, 
        string HomeTeam, string AwayTeam, int HomeScore, int AwayScore, string Progress);
}
