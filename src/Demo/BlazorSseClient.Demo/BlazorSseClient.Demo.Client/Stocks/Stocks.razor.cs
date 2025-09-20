
using BlazorSseClient.Services;
using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BlazorSseClient.Demo.Client.Stocks
{
    public partial class Stocks : ComponentBase, IAsyncDisposable
    {
        [Inject]
        private ISseClient SseClient { get; set; } = null!;

        [Inject]
        private HttpClient HttpClient { get; set; } = null!;

        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private readonly ConcurrentDictionary<string, List<QuoteModel>> _symbols = [];
        private Guid? _quoteSubscriptionId; 
        private const string _messageType = "Quote";
        private readonly List<string> _availableSymbols = [];

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                await GetAvailableSymbolsAsync();
                _quoteSubscriptionId = SseClient.Subscribe(_messageType, AddQuote);
            }
        }

        private void AddQuote(SseEvent sseEvent)
        {
            if (sseEvent.Data is null)
                return;

            try
            {
                var quote = JsonSerializer.Deserialize<QuoteModel?>(sseEvent.Data, _jsonOptions);

                if (quote is null)
                    return;

                if (_symbols.ContainsKey(quote.Value.Symbol))
                {
                    _symbols[quote.Value.Symbol].Add(quote.Value);
                }
                else
                {
                    _symbols.TryAdd(quote.Value.Symbol, new List<QuoteModel>([quote.Value]));
                }

                if (_symbols[quote.Value.Symbol].Count > 20)
                    _symbols[quote.Value.Symbol].RemoveAt(0);

                InvokeAsync(StateHasChanged);
            }
            catch (JsonException)
            {
                // Log or handle the error as needed
                return;
            }
        }

        private async Task GetAvailableSymbolsAsync()
        {
            _availableSymbols.Clear();

            var response = await HttpClient.GetAsync("api/stocks/symbols");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException(content);
            }

            var symbols = JsonSerializer.Deserialize<List<string>>(content, _jsonOptions);


            if (symbols is not null && symbols.Count > 0)
                _availableSymbols.AddRange(symbols.OrderBy(x => x));

            foreach (var symbol in _availableSymbols)
                _symbols.TryAdd(symbol, []);

            StateHasChanged();
        }

        public ValueTask DisposeAsync()
        {
            if (_quoteSubscriptionId is not null)
            {
                SseClient?.Unsubscribe(_messageType, _quoteSubscriptionId.Value);
            }

            return ValueTask.CompletedTask;
        }
    }
}
