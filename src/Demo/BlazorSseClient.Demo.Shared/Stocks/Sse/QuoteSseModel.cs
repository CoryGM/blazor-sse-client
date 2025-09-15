namespace BlazorSseClient.Demo.Shared.Stocks.Sse
{
    public readonly record struct QuoteSseModel(string Symbol, decimal Price, decimal Change,
        decimal ChangePercent, DateTime Timestamp);
}
