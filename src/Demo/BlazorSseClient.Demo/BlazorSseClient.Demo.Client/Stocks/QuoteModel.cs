namespace BlazorSseClient.Demo.Client.Stocks
{
    public readonly record struct QuoteModel(Guid Id, string Symbol, decimal Price, decimal Change,
        decimal ChangePercent, DateTime Timestamp);
}
