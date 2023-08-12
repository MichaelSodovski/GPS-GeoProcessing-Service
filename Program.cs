using WGS_To_ITM_GeoCoding_Service.Services;
using Serilog;


namespace WGS_To_ITM_GeoCoding_Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            string logFilePath = builder.Configuration.GetValue<string>("Logging:FilePath");
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

            try
            {
                Log.Information("Starting Service...");
                
                builder.Services.AddWindowsService(options => {
                    options.ServiceName = "GPSGeoProcessingService";
                });

                // Register DatabaseService in the DI container
                var connectionString = builder.Configuration.GetConnectionString("IRAILMETA");
                builder.Services.AddSingleton(new DatabaseService(connectionString));

                // Register Worker service (BackgroundService)
                builder.Services.AddHostedService<Worker>();

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                }

                //app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.Information("Service stopped...");
                Log.CloseAndFlush();
            }
        }
    }
}
