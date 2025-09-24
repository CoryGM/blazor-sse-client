namespace BlazorSseClient.Demo.Api.Stocks.Data
{
    public interface IStockService
    {
        Quote GetNextQuote();
        Quote GetQuote(string symbol);
        Quote GetRandomQuote();
        Quote GetMostRecentQuote(string symbol);
        IEnumerable<Quote> GetQuoteHistory(string symbol);
        IEnumerable<string> GetSymbols();
    }
}