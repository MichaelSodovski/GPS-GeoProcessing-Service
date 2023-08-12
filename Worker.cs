using Serilog;
using WGS_To_ITM_GeoCoding_Service.Services;


namespace WGS_To_ITM_GeoCoding_Service
{
    public class Worker : BackgroundService
    {
        private readonly DatabaseService _dbService;
        
        public Worker(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() => Log.Information("GPS GeoProcessing Service is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Log.Information("Doing: PerformDataBaseOperations");
                    await _dbService.PerformDataBaseOperations();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred while executing the service logic.");
                }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}