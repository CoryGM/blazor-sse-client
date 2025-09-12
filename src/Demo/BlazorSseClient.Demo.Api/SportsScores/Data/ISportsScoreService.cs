namespace BlazorSseClient.Demo.Api.SportsScores.Data
{
    public interface ISportsScoreService
    {
        string[] Sports { get; }

        Score GetRandomScore();
        Score GetScoreForSport(string sport);
    }
}