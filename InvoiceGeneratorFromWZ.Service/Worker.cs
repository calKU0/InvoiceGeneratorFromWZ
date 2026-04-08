using InvoiceGeneratorFromWZ.Contracts.Settings;
using InvoiceGeneratorFromWZ.Contracts.Services;
using InvoiceGeneratorFromWZ.Service.Services;
using Microsoft.Extensions.Options;

namespace InvoiceGeneratorFromWZ.Service
{
    public class Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        IOptions<AppSettings> appSettings,
        IXlApiService xlApiService) : BackgroundService
    {
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting worker service and logging into XL API.");
            xlApiService.Login();
            logger.LogInformation("Logged into XL API.");

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = appSettings.Value;

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                logger.LogInformation("Starting invoice generation.");

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

                now = DateTime.Now;
                logger.LogInformation("Ended invoice generation.");

                await Task.Delay(TimeSpan.FromMinutes(settings.WorkingIntervalMinutes), stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                xlApiService.Logout();
                logger.LogInformation("Logged out from XL API.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to logout from XL API during service shutdown.");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
