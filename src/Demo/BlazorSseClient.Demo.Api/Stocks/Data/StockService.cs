using System;

namespace BlazorSseClient.Demo.Api.Stocks.Data
{
    public class StockService : IStockService
    {
        private readonly Dictionary<string, List<Quote>> _quoteCache = [];
        private readonly TickerSymbol _tickerSymbol = new();

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
        /// Get the history for a given symbol.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public IEnumerable<Quote> GetQuoteHistory(string symbol)
        {
            if (_quoteCache.TryGetValue(symbol, out var quotes))
                return [.. quotes];

            return [];
        }

        /// <summary>
        /// Get a list of all available symbols.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSymbols()
        {
            return _tickerSymbol.StockSymbols;
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

            var changePercent = GetRandomChangePercent(previousQuote?.ChangePercent);
            var newPrice = previousQuote!.Value.Price * (1 + changePercent);
            var change = newPrice - previousQuote.Value.Price;

            var newQuote = new Quote(symbol, newPrice, change, changePercent, DateTime.UtcNow);

            AddToHistory(newQuote);

            return newQuote;
        }

        private Quote? GetPreviousQuote(string symbol)
        {
            if (_quoteCache.ContainsKey(symbol) && _quoteCache[symbol].Count > 0)
            {
                return _quoteCache[symbol].Last();
            }

            return null;
        }

        private void AddToHistory(Quote quote)
        {
            if (!_quoteCache.ContainsKey(quote.Symbol))
                _quoteCache[quote.Symbol] = [];

            _quoteCache[quote.Symbol].Add(quote);

            // Keep only the last 100 quotes for each symbol
            if (_quoteCache[quote.Symbol].Count > 100)
                _quoteCache[quote.Symbol].RemoveAt(0);
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
        private decimal GetRandomChangePercent(decimal? previousChangePercent = 0m)
        {
            var swingPotential = Random.Shared.Next(1, 100);

            //  Determine the maximum possible change for this iteration.
            decimal max = swingPotential switch
            {
                <= 50 => 0.5m,
                <= 60 => 0.75m,
                <= 70 => 1.0m,
                <= 80 => 1.5m,
                <= 90 => 2.0m,
                <= 95 => 3.0m,
                <= 99 => 5.0m,
                _ => 99.0m
            };

            //  If the previous change was negative, bias the next change to be negative as well.
            var previousDirection = previousChangePercent < 0 ? -1.0m : 1.0m; 
            var nextDirection = previousDirection;
            var directionBiasChance = Random.Shared.Next(1, 100);

            // 92% chance to keep the same direction.
            if (directionBiasChance > 92)
                nextDirection *= -1.0m;
            
            decimal absRandomPercentage = (decimal)(Random.Shared.NextDouble()) * max / 100.0m;

            return absRandomPercentage * nextDirection; 
        }
    }
}
