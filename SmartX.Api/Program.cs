using SmartX.Api.Services;

namespace SmartX.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            builder.Services.AddSingleton<SensorTableService>();

            // Add OpenAPI support.
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Initialize Azure Table Storage
            using (var scope = app.Services.CreateScope())
            {
                var sensorTableService =
                    scope.ServiceProvider.GetRequiredService<SensorTableService>();

                await sensorTableService.InitializeAsync();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // HTTPS redirection is temporarily disabled
            // so we can test the API over HTTP.
            // app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}