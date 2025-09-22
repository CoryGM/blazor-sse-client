using BlazorSseClient.Demo.Api.Locations.Data;

namespace BlazorSseClient.Demo.Api.Weather.Data
{
    public class WeatherService : IWeatherService
    {
        private readonly Dictionary<string, List<CurrentWeather>> _weatherCache = [];
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

                var reading = new CurrentWeather
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
                    TakenAtUtc = DateTime.UtcNow,
                    ReadingNumber = 1
                };

                var cachedReading = AddToCache(reading);

                return cachedReading;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error fetching weather data for {City}", location.Name);
                return null;
            }
        }

        public IEnumerable<CurrentWeather> GetWeatherHistory(string city)
        {
            var key = GetNormalizedKeyName(city);

            if (String.IsNullOrEmpty(key))
                return [];

            if (_weatherCache.TryGetValue(key, out var cachedWeather))
                return [.. cachedWeather];

            return [];
        }

        public IEnumerable<CurrentWeather> GetMostRecentReadings()
        {
            var mostRecentReadings = new List<CurrentWeather>();

            foreach (var key in _weatherCache.Keys)
            {
                var mostRecent = GetMostRecentReading(key);

                if (mostRecent is not null)
                    mostRecentReadings.Add(mostRecent.Value);
            }

            return mostRecentReadings;  
        }

        public CurrentWeather? GetMostRecentReading(string city)
        {
            var key = GetNormalizedKeyName(city);

            if (String.IsNullOrEmpty(key))
                return null;

            if (_weatherCache.TryGetValue(key, out var cachedWeather))
            {
                if (cachedWeather.Count == 0)
                    return null;

                var mostRecentTimeStamp = cachedWeather.Max(x => x.TakenAtUtc);
                var mostRecent = cachedWeather.FirstOrDefault(x => x.TakenAtUtc == mostRecentTimeStamp);    

                return mostRecent;
            }

            return null;
        }

        private CurrentWeather? AddToCache(CurrentWeather weather)
        {
            var key = GetNormalizedKeyName(weather.City);

            if (String.IsNullOrEmpty(key))
                return null;

            if (_weatherCache.TryGetValue(key, out List<CurrentWeather>? value))
            {
                var mostRecentReading = GetMostRecentReading(key);

                if (value.Count == 0)
                {
                    value.Add(weather);
                    return null;
                }

                var maxReadingNumber = value.Count > 0 ? value.Max(x => x.ReadingNumber) : 0;
                var newWeatherRecord = weather with { ReadingNumber = maxReadingNumber + 1 };
                value.Add(newWeatherRecord);

                // Keep only the latest 25 entries
                while (value.Count > 25)
                    value.RemoveAt(0);

                return newWeatherRecord;
            }
            else
            {
                _weatherCache[key] = [];
                _weatherCache[key].Add(weather);
                return weather;
            }
        }

        private static string? GetNormalizedKeyName(string? key)
        {
            if (String.IsNullOrWhiteSpace(key))
                return String.Empty;

            return key.Trim().ToLowerInvariant();
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
