using BlazorSseClient.Services;
using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http.Json;
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
        private readonly ConcurrentDictionary<string, List<QuoteModel>> _quoteCache = [];
        private Guid? _quoteSubscriptionId; 
        private const string _messageType = "Quote";
        private readonly List<string> _availableSymbols = [];
        private string renderLocation = String.Empty;

        protected override void OnInitialized()
        {
            if (OperatingSystem.IsBrowser())
                renderLocation = "Browser";
            else
                renderLocation = "Server";
        }

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
            if (OperatingSystem.IsBrowser())
                renderLocation = "Browser";
            else
                renderLocation = "Server";

            if (sseEvent.Data is null)
                return;

            try
            {
                var quote = JsonSerializer.Deserialize<QuoteModel?>(sseEvent.Data, _jsonOptions);

                if (quote is null)
                    return;

                AddQuoteInternal(quote.Value);

                InvokeAsync(StateHasChanged);
            }
            catch (JsonException)
            {
                // Log or handle the error as needed
                return;
            }
        }

        private void AddQuoteInternal(QuoteModel quote, bool trim = true)
        {
            if (_quoteCache.TryGetValue(quote.Symbol, out List<QuoteModel>? value))
            {
                if (value.Any(x => x.Id == quote.Id))
                    return;

                value.Add(quote);
            }
            else
            {
                _ = _quoteCache.TryAdd(quote.Symbol, new List<QuoteModel>([quote]));
            }

            if (trim)
                TrimToMax(_quoteCache[quote.Symbol]);
        }

        private static void TrimToMax(List<QuoteModel> list, int max = 20)
        {
            list.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));

            if (list.Count > max)
                list.RemoveRange(max, list.Count - max);
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
            {
                if (_quoteCache.TryAdd(symbol, []))
                    await LoadQuoteHistory(symbol);
            }

            StateHasChanged();
        }

        private async Task LoadQuoteHistory(string? symbol)
        {
            if (String.IsNullOrWhiteSpace(symbol))
                return;

            var response = await HttpClient.GetAsync($"api/stocks/quotes/{symbol}/history");

            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException();
            }

            var quotes = await response.Content.ReadFromJsonAsync<IEnumerable<QuoteModel>>();

            if (quotes is null)
                return;

            var trim = false;

            for (var i = 0; i < quotes.Count(); i++)
            {
                if (i == quotes.Count() - 1)
                    trim = true;

                AddQuoteInternal(quotes.ElementAt(i), trim);
            }
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
