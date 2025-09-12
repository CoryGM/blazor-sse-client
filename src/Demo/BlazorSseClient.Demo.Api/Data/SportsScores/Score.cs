namespace BlazorSseClient.Demo.Api.Data.SportsScores
{
    public record struct Score(string Sport, string HomeTeam, string AwayTeam, int HomeScore, int AwayScore, string Progress);
}
