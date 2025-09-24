using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlazorSseClient.Wasm
{
    public static class WasmSseServiceCollectionExtensions
    {
        public static IServiceCollection AddWasmSseClient(this IServiceCollection services,
            Action<WasmSseClientOptions>? configure)
        {
            if (configure != null)
                services.Configure(configure);

            services.AddSingleton(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<WasmSseClientOptions>>().Value;
                opts.Validate();

                return opts;
            });

            services.AddSingleton<ISseClient, WasmSseClient>();

            return services;
        }
    }
}
