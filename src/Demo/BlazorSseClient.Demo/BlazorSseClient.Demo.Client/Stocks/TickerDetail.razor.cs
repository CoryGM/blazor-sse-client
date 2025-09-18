using Microsoft.AspNetCore.Components;

namespace BlazorSseClient.Demo.Client.Stocks
{
    public partial class TickerDetail : ComponentBase
    {
        [Parameter]
        public string Symbol { get; set; } = string.Empty;

        [Parameter]
        public IEnumerable<QuoteModel>? Quotes { get; set; }

        
        protected override void OnParametersSet()
        {
            if (string.IsNullOrWhiteSpace(Symbol))
                throw new ArgumentException("Symbol parameter is required.", nameof(Symbol));

            Quotes ??= [];
        }
    }
}
