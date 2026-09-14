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
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("SmartXClient", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5221")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // Azure Table Storage
            builder.Services.AddSingleton<SensorTableService>();
            builder.Services.AddSingleton<TelemetryService>();

            // Telemetry processing
            builder.Services.AddSingleton<TelemetryBatchProcessor>();
            builder.Services.AddSingleton<TelemetryPacketProcessor>();
            builder.Services.AddSingleton<MockTelemetryGenerator>();

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