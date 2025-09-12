using System;

namespace BlazorSseClient.Demo.Api.Data.Stocks
{
    public class StockService : IStockService
    {
        private Dictionary<string, List<Quote>> _quoteHistory = [];
        private TickerSymbol _tickerSymbol = new TickerSymbol();

        /// <summary>
        /// Gets a quote for a random symbol from the list.
        /// </summary>
        /// <returns></returns>
        public Quote GetRandomQuote()
        {
            var symbol = _tickerSymbol.GetRandomSymbol();
            return GetQuote(symbol);
        }

        /// <summary>
        /// Gets a quote for the next symbol in the list.
        /// </summary>
        /// <returns></returns>
        public Quote GetNextQuote()
        {
            var symbol = _tickerSymbol.GetNextSymbol();
            return GetQuote(symbol);
        }

        /// <summary>
        /// Gets a new quote for the specified symbol.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public Quote GetQuote(string symbol)
        {
            // In a real application, you would get the quote from a database or an external service.
            // Here, we just return a dummy quote.
            var previousQuote = GetPreviousQuote(symbol);

            previousQuote ??= new Quote
            {
                Symbol = symbol,
                Price = GetRandomStockPrice(),
                Change = 0.0m,
                ChangePercent = 0.0m,
                Timestamp = DateTime.UtcNow
            };

            var changePercent = GetRandomChangePercent();
            var newPrice = previousQuote.Value.Price * (1 + changePercent);
            var change = newPrice - previousQuote.Value.Price;

            var newQuote = new Quote(symbol, newPrice, change, changePercent, DateTime.UtcNow);

            AddToHistory(newQuote);

            return newQuote;
        }

        private Quote? GetPreviousQuote(string symbol)
        {
            if (_quoteHistory.ContainsKey(symbol) && _quoteHistory[symbol].Count > 0)
            {
                return _quoteHistory[symbol].Last();
            }

            return null;
        }

        private void AddToHistory(Quote quote)
        {
            if (!_quoteHistory.ContainsKey(quote.Symbol))
                _quoteHistory[quote.Symbol] = [];

            _quoteHistory[quote.Symbol].Add(quote);

            // Keep only the last 100 quotes for each symbol
            if (_quoteHistory[quote.Symbol].Count > 100)
                _quoteHistory[quote.Symbol].RemoveAt(0);
        }

        private decimal GetRandomStockPrice()
        {
            var random = new Random();
            decimal min = 75.0m;
            decimal max = 250.0m;

            decimal randomDecimal = (decimal)(random.NextDouble() * (double)(max - min)) + min;

            return randomDecimal;
        }

        /// <summary>
        /// Gets a random percentage change between -99% and +99%, with a bias 
        /// towards smaller changes.
        /// </summary>
        /// <returns></returns>
        private decimal GetRandomChangePercent()
        {
            var random = new Random();
            var swingPotential = random.Next(1, 100);

            decimal max = swingPotential switch
            {
                <= 50 => 10.0m,
                <= 60 => 15.0m,
                <= 70 => 20.0m,
                <= 80 => 25.0m,
                <= 90 => 35.0m,
                <= 95 => 50.0m,
                <= 99 => 75.0m,
                _ => 99.0m
            };

            decimal min = max * -1;
            decimal randomPercentage = ((decimal)(random.NextDouble() * (double)(max - min)) + min) / 100;

            return randomPercentage;
        }
    }
}
