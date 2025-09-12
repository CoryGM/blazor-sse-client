namespace BlazorSseClient.Demo.Api.Data.Stocks
{
    public record struct Quote(string Symbol, decimal Price, decimal Change, 
        decimal ChangePercent, DateTime Timestamp);
}
