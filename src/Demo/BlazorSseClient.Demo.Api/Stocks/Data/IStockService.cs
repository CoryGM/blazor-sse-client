namespace BlazorSseClient.Demo.Api.Stocks.Data
{
    public interface IStockService
    {
        Quote GetNextQuote();
        Quote GetQuote(string symbol);
        Quote GetRandomQuote();
    }
}