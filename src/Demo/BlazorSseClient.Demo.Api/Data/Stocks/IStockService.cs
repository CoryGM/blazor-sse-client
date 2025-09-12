namespace BlazorSseClient.Demo.Api.Data.Stocks
{
    public interface IStockService
    {
        Quote GetNextQuote();
        Quote GetQuote(string symbol);
        Quote GetRandomQuote();
    }
}