using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using BlazorSseClient.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddScoped(sp =>
{
    var baseAddress = builder.Configuration["Api:BaseAddress"];

    if (String.IsNullOrEmpty(baseAddress))
        throw new InvalidOperationException("Api:BaseAddress configuration value is missing or null.");

    return new HttpClient { BaseAddress = new Uri(baseAddress) };
});

builder.Services.AddWasmSseClient(options =>
{
    options.BaseAddress = builder.Configuration["Sse:BaseAddress"];
    options.ReconnectBaseDelayMs = 1000;
    options.ReconnectMaxDelayMs = 15000;
    options.UseCredentials = false;
    options.AutoStart = true;
});

var host = builder.Build();

await host.RunAsync();
