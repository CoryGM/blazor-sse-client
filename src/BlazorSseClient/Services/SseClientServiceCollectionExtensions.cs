using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlazorSseClient.Services;

public static class SseClientServiceCollectionExtensions
{
    public static IServiceCollection AddSseClient(this IServiceCollection services)
    {
        services.TryAddScoped<ISseClient, SseClient>();
        return services;
    }
}