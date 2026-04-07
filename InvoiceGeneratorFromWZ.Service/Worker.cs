using InvoiceGeneratorFromWZ.Contracts.Settings;
using InvoiceGeneratorFromWZ.Service.Services;
using Microsoft.Extensions.Options;

namespace InvoiceGeneratorFromWZ.Service
{
    public class Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IOptions<AppSettings> appSettings) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = appSettings.Value;

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;


                logger.LogInformation("Starting invoice generation at: {time}", now);

                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceProcessingService>();
                    await invoiceService.ProcessInvoicesAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred while processing invoices.");
                }


                await Task.Delay(TimeSpan.FromMinutes(settings.WorkingIntervalMinutes), stoppingToken);
            }
        }
    }
}
