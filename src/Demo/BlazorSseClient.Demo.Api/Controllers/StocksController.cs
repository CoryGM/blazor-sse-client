using Microsoft.AspNetCore.Mvc;

using BlazorSseClient.Demo.Api.Stocks.Data;

namespace BlazorSseClient.Demo.Api.Controllers
{
    [Route("api/stocks")]
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

        [HttpGet("quote/{symbol}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Quote))]
        public ActionResult<Quote?> GetQuote(string symbol)
        {
            _logger?.LogDebug("GetQuote: {symbol}", symbol);

            if (String.IsNullOrWhiteSpace(symbol))
                return BadRequest("Symbol is required.");

            var quote = _service.GetQuote(symbol);

            return Ok(quote);
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
