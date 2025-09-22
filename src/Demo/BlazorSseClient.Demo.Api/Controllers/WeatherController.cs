using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

using BlazorSseClient.Demo.Api.Weather.Data;

namespace BlazorSseClient.Demo.Api.Controllers
{
    [Route("api/weather")]
    [EnableCors("SseCorsPolicy")]
    [ApiController]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _service;
        private readonly ILogger<WeatherController>? _logger;

        public WeatherController(IWeatherService service, ILogger<WeatherController> logger)
        {
            ArgumentNullException.ThrowIfNull(service, nameof(service));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _service = service;
            _logger = logger;
        }

        [HttpGet("readings/{city}/most-recent")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CurrentWeather))]
        public ActionResult<CurrentWeather> GetMostRecentReading(string? city)
        {
            _logger?.LogDebug("GetMostRecentReading: {city}", city);

            if (String.IsNullOrWhiteSpace(city))
                return BadRequest("City is required.");

            var reading = _service.GetMostRecentReading(city);

            if (reading is null)
                return NotFound();

            return Ok(reading);
        }

        [HttpGet("readings/most-recent")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CurrentWeather>))]
        public ActionResult<IEnumerable<CurrentWeather>> GetMostRecentReadings()
        {
            _logger?.LogDebug("GetMostRecentReadings");

            var readings = _service.GetMostRecentReadings();

            return Ok(readings);
        }
    }
}
