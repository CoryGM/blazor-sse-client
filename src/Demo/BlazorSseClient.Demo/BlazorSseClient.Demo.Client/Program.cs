using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using BlazorSseClient.Services;
using BlazorSseClient.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure HttpClient to connect to the api using service discovery
builder.Services.AddSingleton<ISseClient, WasmSseClient>();

var host = builder.Build();

await host.Services.GetRequiredService<ISseClient>()
    .StartAsync("https://localhost:7290/api/stream/messages");

await host.RunAsync();
