using System.Text.Json;

using BlazorSseClient.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorSseClient.Demo.Client.SportsScores
{
    public partial class ScoreBanner : ComponentBase, IAsyncDisposable
    {

        [Inject]
        private ISseClient SseClient { get; set; } = null!;

        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web); 
        private List<ScoreModel> _scores { get; set; } = [];
        private const string _messageType = "Score";
        private Guid? _scoreSubscriptionId;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                _scoreSubscriptionId = SseClient.Subscribe(_messageType, AddScore);
            }
        }

        private void AddScore(SseEvent sseEvent)
        {
            try
            {
                var quote = JsonSerializer.Deserialize<ScoreModel?>(sseEvent.Data, _jsonOptions);

                if (quote is null)
                    return;

                if (_scores.Any(q => q.Id == quote.Value.Id))
                    return;

                //if (_scores.Count < 3)
                //{
                    _scores.Add(quote.Value);

                    if (_scores.Count > 10)
                        _scores.RemoveAt(0);

                    InvokeAsync(StateHasChanged);
                //}
            }
            catch (JsonException)
            {
                // Log or handle the error as needed
                return;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_scoreSubscriptionId.HasValue)
            {
                SseClient.Unsubscribe(_messageType, _scoreSubscriptionId.Value);
                _scoreSubscriptionId = null;
            }
        }
    }
}
