
using SmartX.Api.Services;

namespace SmartX.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            // Allow the SmartX.Client Blazor app to call the API
            var clientOrigin = builder.Configuration["Client:BaseUrl"] ?? "http://localhost:5221";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("SmartXClient", policy =>
                {
                    policy.WithOrigins(clientOrigin).AllowAnyHeader().AllowAnyMethod();
                });
            });

            // Azure Table Storage
            builder.Services.AddSingleton<SensorTableService>();
            builder.Services.AddSingleton<TelemetryService>();
            builder.Services.AddSingleton<CommandStreamService>();

            // Telemetry processing
            builder.Services.AddSingleton<TelemetryBatchProcessor>();
            builder.Services.AddSingleton<TelemetryPacketProcessor>();
            builder.Services.AddSingleton<MockTelemetryGenerator>();
            builder.Services.AddSingleton<TelemetryAnomalyService>();
            builder.Services.AddSingleton<SensorHealthService>();

            // Recursive deployment validation
            builder.Services.AddSingleton<DeploymentValidationService>();

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

                var commandStreamService =
                    scope.ServiceProvider.GetRequiredService<CommandStreamService>();

                var registeredSensors = await sensorTableService.GetSensorsAsync();
                foreach (var sensor in registeredSensors)
                    commandStreamService.RegisterDevice(sensor.MacAddress, sensor.Category);

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

            // Enable CORS before authorization and controllers
            app.UseCors("SmartXClient");

            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
