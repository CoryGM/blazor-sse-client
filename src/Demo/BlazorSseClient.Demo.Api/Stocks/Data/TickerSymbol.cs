namespace BlazorSseClient.Demo.Api.Stocks.Data
{
    public class TickerSymbol
    {
        private int _currentTickerIndex = 0;

        private string[] stockSymbols = 
        [
            "AAPL", "AMZN", "DIS", "FB", "GOOGL", "MSFT", "NVDA", "TSLA"
        ];

        public IEnumerable<string> StockSymbols => [.. stockSymbols];

        /// <summary>
        /// Gets a random symbol from the list.
        /// </summary>
        /// <returns></returns>
        public string GetRandomSymbol()
        {
            var rnd = new Random();
            int index = rnd.Next(stockSymbols.Length);

            return stockSymbols[index];
        }

        /// <summary>
        /// Gets the next symbol in the list after the given symbol.
        /// Cycles back to the start if at the end of the list.
        /// Returns the first symbol if the given symbol is not found.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public string GetNextSymbol(string symbol)
        {
            var index = Array.IndexOf(stockSymbols, symbol);
            _currentTickerIndex = index + 1;

            return GetNextSymbol();
        }

        /// <summary>
        /// Gets the next symbol in the list after the last one returned.
        /// Cycles back to the start if at the end of the list.
        /// </summary>
        /// <returns></returns>
        public string GetNextSymbol()
        {
            if (_currentTickerIndex >= stockSymbols.Length)
                _currentTickerIndex = 0;

            var symbol = stockSymbols[_currentTickerIndex];
            
            _currentTickerIndex++;

            return symbol;
        }
    }
}
