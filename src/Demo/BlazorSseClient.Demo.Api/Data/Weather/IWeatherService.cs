using BlazorSseClient.Demo.Api.Data.Locations;

namespace BlazorSseClient.Demo.Api.Data.Weather
{
    public interface IWeatherService
    {
        Task<CurrentWeather?> GetCurrentWeatherAsync(Location location, CancellationToken cancellationToken = default);
        Task<CurrentWeather?> GetRandomCurrentWeatherAsync(CancellationToken cancellationToken = default);
    }
}