using BlazorSseClient.Services;
using BlazorSseClient.Wasm;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Headers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure HttpClient to connect to the api using service discovery
builder.Services.AddSingleton<ISseClient, WasmSseClient>();

await builder.Build().RunAsync();
