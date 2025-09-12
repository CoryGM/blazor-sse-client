namespace BlazorSseClient.Demo.Api.Stocks.Data
{
    public readonly record struct Quote(string Symbol, decimal Price, decimal Change, 
        decimal ChangePercent, DateTime Timestamp);
}
