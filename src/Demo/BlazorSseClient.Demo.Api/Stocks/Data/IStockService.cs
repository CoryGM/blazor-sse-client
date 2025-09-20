namespace BlazorSseClient.Demo.Api.Stocks.Data
{
    public interface IStockService
    {
        Quote GetNextQuote();
        Quote GetQuote(string symbol);
        Quote GetRandomQuote();
        IEnumerable<Quote> GetQuoteHistory(string symbol);
        IEnumerable<string> GetSymbols();
    }
}