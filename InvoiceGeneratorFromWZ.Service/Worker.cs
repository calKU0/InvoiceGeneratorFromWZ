using InvoiceGeneratorFromWZ.Contracts.Services;
using InvoiceGeneratorFromWZ.Contracts.Settings;
using InvoiceGeneratorFromWZ.Service.Services;
using Microsoft.Extensions.Options;

namespace InvoiceGeneratorFromWZ.Service
{
    public class Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IOptions<AppSettings> appSettings) : BackgroundService
    {
        private DateTime _lastRunDate = DateTime.MinValue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = appSettings.Value;

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                if (now.Hour == settings.GenerateInvoicesHour && _lastRunDate.Date != now.Date)
                {
                    logger.LogInformation("Starting invoice generation at: {time}", now);

                    try
                    {
                        using var scope = serviceProvider.CreateScope();
                        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceProcessingService>();
                        await invoiceService.ProcessInvoicesAsync();

                        _lastRunDate = now;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error occurred while processing invoices.");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(settings.WorkingIntervalMinutes), stoppingToken);
            }
        }
    }
}
