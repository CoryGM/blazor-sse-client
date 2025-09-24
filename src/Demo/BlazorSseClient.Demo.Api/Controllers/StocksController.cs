using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

using BlazorSseClient.Demo.Api.Stocks.Data;

namespace BlazorSseClient.Demo.Api.Controllers
{
    [Route("api/stocks")]
    [EnableCors("SseCorsPolicy")]
    [ApiController]
    public class StocksController : ControllerBase
    {
        private readonly IStockService _service;
        private readonly ILogger<StocksController>? _logger;

        public StocksController(IStockService service, ILogger<StocksController> logger)
        {
            ArgumentNullException.ThrowIfNull(service, nameof(service));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _service = service;
            _logger = logger;
        }

        [HttpGet("quotes/{symbol}/most-recent")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Quote))]
        public ActionResult<Quote?> GetQuote(string symbol)
        {
            _logger?.LogDebug("GetQuote: {symbol}", symbol);

            if (String.IsNullOrWhiteSpace(symbol))
                return BadRequest("Symbol is required.");

            var quote = _service.GetMostRecentQuote(symbol);

            return Ok(quote);
        }

        [HttpGet("quotes/{symbol}/history")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Quote>))]
        public ActionResult<IEnumerable<Quote>> GetQuoteHistory(string symbol)
        {
            _logger?.LogDebug("GetQuoteHistory: {symbol}", symbol);

            if (String.IsNullOrWhiteSpace(symbol))
                return BadRequest("Symbol is required.");

            var history = _service.GetQuoteHistory(symbol);

            return Ok(history);
        }

        [HttpGet("symbols")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
        public ActionResult<IEnumerable<string>> GetSymbols()
        {
            _logger?.LogDebug("GetSymbols");

            var symbols = _service.GetSymbols();

            return Ok(symbols);
        }
    }
}
