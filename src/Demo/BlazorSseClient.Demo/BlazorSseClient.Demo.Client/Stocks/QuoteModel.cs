namespace BlazorSseClient.Demo.Client.Stocks
{
    public readonly record struct QuoteModel(string Symbol, decimal Price, decimal Change,
        decimal ChangePercent, DateTime Timestamp);
}
