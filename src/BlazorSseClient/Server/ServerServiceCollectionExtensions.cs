using System.Net.Http.Headers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlazorSseClient.Server;

public static class ServerSseServiceCollectionExtensions
{
    public static IServiceCollection AddServerSseClient(this IServiceCollection services,
        Action<ServerSseClientOptions>? configure)
    {
        if (configure != null)
            services.Configure(configure);

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ServerSseClientOptions>>().Value;
            opts.Validate();

            return opts;
        });

        services.AddHttpClient<ISseStreamClient, SseStreamClient>((sp, http) =>
        {
            var opts = sp.GetRequiredService<ServerSseClientOptions>();

            http.Timeout = opts.Timeout;

            if (!http.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "text/event-stream"))
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            foreach (var kv in opts.DefaultRequestHeaders)
                http.DefaultRequestHeaders.TryAddWithoutValidation(kv.Key, kv.Value);
        });

        services.AddSingleton<ISseClient, ServerSseClient>();

        return services;
    }
}