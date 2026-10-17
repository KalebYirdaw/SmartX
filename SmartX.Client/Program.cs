using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SmartX.Client;
using SmartX.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Read API address from wwwroot/appsettings.json so it is not hard-coded here.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5212/";
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

// Register SmartX API services
builder.Services.AddScoped<TelemetryApiService>();
builder.Services.AddScoped<SensorApiService>();

await builder.Build().RunAsync();