using BlazorSseClient.Demo.Api.Locations.Data;

namespace BlazorSseClient.Demo.Api.Weather.Data
{
    public interface IWeatherService
    {
        Task<CurrentWeather?> GetCurrentWeatherAsync(Location location, CancellationToken cancellationToken = default);
        Task<CurrentWeather?> GetRandomCurrentWeatherAsync(CancellationToken cancellationToken = default);
    }
}