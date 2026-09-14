
using SmartX.Api.Services;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SmartX.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            // Azure Table Storage
            builder.Services.AddSingleton<SensorTableService>();
            builder.Services.AddSingleton<TelemetryService>();

            // Azure File Share
            builder.Services.AddSingleton<SensorFileService>();

            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Initialize Azure Storage
            using (var scope = app.Services.CreateScope())
            {
                var sensorTableService =
                    scope.ServiceProvider.GetRequiredService<SensorTableService>();

                await sensorTableService.InitializeAsync();

                var telemetryService =
                    scope.ServiceProvider.GetRequiredService<TelemetryService>();

                await telemetryService.InitializeAsync();

                var sensorFileService =
                    scope.ServiceProvider.GetRequiredService<SensorFileService>();

                await sensorFileService.InitializeAsync();
            }

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // HTTPS temporarily disabled for local HTTP testing.
            // app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
