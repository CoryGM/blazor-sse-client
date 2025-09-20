using BlazorSseClient.Demo.Api.Locations.Data;

namespace BlazorSseClient.Demo.Api.Weather.Data
{
    public class WeatherService : IWeatherService
    {
        private readonly Dictionary<string, List<CurrentWeather>> _weatherCache = new();
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(IHttpClientFactory httpClientFactory, ILogger<WeatherService> logger)
        {
            ArgumentNullException.ThrowIfNull(httpClientFactory, nameof(httpClientFactory));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<CurrentWeather?> GetRandomCurrentWeatherAsync(CancellationToken cancellationToken = default)
        {
            var location = Cities.GetRandomCity();
            return await GetCurrentWeatherAsync(location, cancellationToken);
        }

        public async Task<CurrentWeather?> GetCurrentWeatherAsync(Location location,
            CancellationToken cancellationToken = default)
        {
            var httpClient = _httpClientFactory.CreateClient("WeatherApi");
            var url = $"v1/forecast?latitude={location.Latitude}" +
                $"&longitude={location.Longitude}" +
                "&current=temperature_2m,relative_humidity_2m,is_day,wind_speed_10m,wind_direction_10m,wind_gusts_10m,weather_code,precipitation,apparent_temperature" +
                "&forecast_days=1" +
                "&wind_speed_unit=mph" +
                "&temperature_unit=fahrenheit" +
                "&precipitation_unit=inch";
            
            try
            {
                var response = await httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return null;

                var weatherResponse = await response.Content.ReadFromJsonAsync<Reading>(cancellationToken: cancellationToken);

                if (weatherResponse == null)
                    return null;

                var reading =  new CurrentWeather
                {
                    City = location.Name,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Temperature = $"{weatherResponse.Current.Temperature2m}{weatherResponse.Units.Temperature2m}",
                    RelativeHumidity = $"{weatherResponse.Current.RelativeHumidity2m}{weatherResponse.Units.RelativeHumidity2m}",
                    ApparentTemperature = $"{weatherResponse.Current.ApparentTemperature}{weatherResponse.Units.ApparentTemperature}",
                    IsDayTime = weatherResponse.Current.IsDay,
                    WindSpeed = $"{weatherResponse.Current.WindSpeed10m}{weatherResponse.Units.WindSpeed10m}",
                    WindDirection = $"{weatherResponse.Current.WindDirection10m}° {GetWindDirection(weatherResponse.Current.WindDirection10m)}",
                    WindGusts = $"{weatherResponse.Current.WindGusts10m}{weatherResponse.Units.WindGusts10m}",
                    Precipitation = $"{weatherResponse.Current.Precipitation} {weatherResponse.Units.Precipitation}",
                    TakenAtUtc = DateTime.UtcNow
                };

                AddToCache(location.Name, reading);

                return reading;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error fetching weather data for {City}", location.Name);
                return null;
            }
        }

        public IEnumerable<CurrentWeather> GetWeatherHistory(string city)
        {
            if (_weatherCache.TryGetValue(city, out var cachedWeather))
                return [.. cachedWeather];

            return [];
        }

        private void AddToCache(string city, CurrentWeather weather)
        {
            if (!_weatherCache.ContainsKey(city))
                _weatherCache[city] = [];

            _weatherCache[city].Add(weather);

            // Keep only the latest 25 entries
            if (_weatherCache[city].Count > 25)
                _weatherCache[city].RemoveAt(0);
        }

        private static string GetWindDirection(double degrees)
        {
            string[] directions =
            {
                "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
                "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"
            };

            int index = (int)((degrees + 11.25) / 22.5) % 16;

            return directions[index];
        }
    }
}
