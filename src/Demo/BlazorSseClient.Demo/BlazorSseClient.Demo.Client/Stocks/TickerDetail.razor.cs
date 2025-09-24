using Microsoft.AspNetCore.Components;

namespace BlazorSseClient.Demo.Client.Stocks
{
    public partial class TickerDetail : ComponentBase
    {
        [Parameter]
        public string Symbol { get; set; } = String.Empty;

        [Parameter]
        public IEnumerable<QuoteModel>? Quotes { get; set; }

        private List<QuoteModel> DisplayQuotes = [];
        private List<string> FillerLines = [];
        private DateTime? LastUpdated = null;

        protected override void OnParametersSet()
        {
            if (String.IsNullOrWhiteSpace(Symbol))
                throw new ArgumentException("Symbol parameter is required.", nameof(Symbol));

            FillerLines.Clear();
            DisplayQuotes.Clear();

            Quotes ??= [];

            DisplayQuotes.AddRange([.. Quotes.Take(5)]);

            if (Quotes.Any())
                LastUpdated = Quotes.Max(q => q.Timestamp);
            else
                LastUpdated = null;

            var fillerCount = 5 - DisplayQuotes.Count;
            FillerLines.AddRange(Enumerable.Repeat(String.Empty, fillerCount));

            if (fillerCount == 5)
                FillerLines[0] = "No quotes received yet...";
        }
    }
}
