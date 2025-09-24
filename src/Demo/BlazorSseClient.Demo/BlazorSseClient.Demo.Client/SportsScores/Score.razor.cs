using Microsoft.AspNetCore.Components;

namespace BlazorSseClient.Demo.Client.SportsScores
{
    public partial class Score : ComponentBase
    {
        [Parameter]
        public ScoreModel ScoreModel { get; set; }

        private string _sportIcon = "🏅";

        protected override void OnParametersSet()
        {
            _sportIcon = GetSportIcon();
        }

        private string GetSportIcon()
        {
            return ScoreModel.Sport.ToLower() switch
            {
                "soccer" => "⚽",
                "basketball" => "🏀",
                "baseball" => "⚾",
                "football" => "🏈",
                "tennis" => "🎾",
                "hockey" => "🏒",
                _ => "🏅"
            };
        }
    }
}
