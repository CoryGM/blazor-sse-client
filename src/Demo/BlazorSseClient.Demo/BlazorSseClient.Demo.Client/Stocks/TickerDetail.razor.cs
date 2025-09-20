using Microsoft.AspNetCore.Components;

namespace BlazorSseClient.Demo.Client.Stocks
{
    public partial class TickerDetail : ComponentBase
    {
        [Parameter]
        public string Symbol { get; set; } = String.Empty;

        [Parameter]
        public IEnumerable<QuoteModel>? Quotes { get; set; }

        private IEnumerable<QuoteModel> DisplayQuotes = [];
        private DateTime? LastUpdated = null;


        protected override void OnParametersSet()
        {
            if (String.IsNullOrWhiteSpace(Symbol))
                throw new ArgumentException("Symbol parameter is required.", nameof(Symbol));

            Quotes ??= [];

            DisplayQuotes = Quotes
                .OrderByDescending(q => q.Timestamp)
                .Take(5)
                .ToList();

            if (Quotes.Any())
                LastUpdated = Quotes.Max(q => q.Timestamp);
            else
                LastUpdated = null;
        }
    }
}
