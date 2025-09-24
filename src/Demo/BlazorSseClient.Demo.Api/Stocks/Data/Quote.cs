namespace BlazorSseClient.Demo.Api.Stocks.Data
{
    public readonly record struct Quote(Guid Id, string Symbol, decimal Price, decimal Change, 
        decimal ChangePercent, DateTime Timestamp);
}
