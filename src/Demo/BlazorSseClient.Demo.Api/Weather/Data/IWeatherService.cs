using BlazorSseClient.Demo.Api.Locations.Data;

namespace BlazorSseClient.Demo.Api.Weather.Data
{
    public interface IWeatherService
    {
        IEnumerable<CurrentWeather> GetWeatherHistory(string city);
        Task<CurrentWeather?> GetCurrentWeatherAsync(Location location, CancellationToken cancellationToken = default);
        Task<CurrentWeather?> GetRandomCurrentWeatherAsync(CancellationToken cancellationToken = default);
    }
}