using InvoiceGeneratorFromWZ.Contracts.Repositories;
using InvoiceGeneratorFromWZ.Contracts.Services;
using InvoiceGeneratorFromWZ.Contracts.Settings;
using Microsoft.Extensions.Options;

namespace InvoiceGeneratorFromWZ.Service.Services
{
    public class InvoiceProcessingService : IInvoiceProcessingService
    {
        private readonly IDocumentRespository _documentRepository;
        private readonly IXlApiService _xlApiService;
        private readonly ILogger<InvoiceProcessingService> _logger;
        private readonly Dictionary<string, int> _wzGenerationStartHours;
        private readonly int _fallbackStartHour;

        public InvoiceProcessingService(
            IDocumentRespository documentRepository,
            IXlApiService xlApiService,
            ILogger<InvoiceProcessingService> logger,
            IOptions<List<WZGenerationTimes>> wzGenerationTimesOptions)
        {
            _documentRepository = documentRepository;
            _xlApiService = xlApiService;
            _logger = logger;

            var generationTimes = wzGenerationTimesOptions.Value ?? [];
            _fallbackStartHour = generationTimes.FirstOrDefault(x => x.IsFallback)?.StartHour ?? 18;

            _wzGenerationStartHours = generationTimes
                .Where(x => !x.IsFallback && !string.IsNullOrWhiteSpace(x.Courier))
                .GroupBy(x => x.Courier.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().StartHour, StringComparer.OrdinalIgnoreCase);
        }

        public async Task ProcessInvoicesAsync()
        {
            try
            {
                var documents = await _documentRepository.GetWZDocuments();
                var today = DateTime.Now.Day;
                var currentHour = DateTime.Now.Hour;

                var toInvoice = documents
                    .Where(d => d.ShouldInvoiceToday(today))
                    .Where(d => CanGenerateForCourier(d.Courier, currentHour))
                    .ToList();

                if (!toInvoice.Any())
                {
                    _logger.LogInformation("No documents found for invoicing at this time.");
                    return;
                }

                // Group: Acronym, Courier, PaymentType, PaymentDate, AddressId, ClientId
                var grouped = toInvoice.GroupBy(d => new
                {
                    d.ClientAcronym,
                    d.Courier,
                    d.PaymentNumber,
                    d.PaymentDueDate,
                    d.AddressId,
                    d.ClientId
                }).ToList();

                _logger.LogInformation("Found {Count} groups to process.", grouped.Count);

                foreach (var group in grouped)
                {
                    try
                    {
                        var wzList = group.ToList();
                        _xlApiService.CreateInvoice(wzList);
                        _logger.LogInformation("Created invoice for Client {Acronym}, Count {Docs}", group.Key.ClientAcronym, wzList.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create invoice for group {@GroupKey}", group.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing invoices.");
            }
        }

        private bool CanGenerateForCourier(string courier, int currentHour)
        {
            if (string.IsNullOrWhiteSpace(courier))
            {
                return true;
            }

            var normalizedCourier = courier.Trim();

            if (_wzGenerationStartHours.TryGetValue(normalizedCourier, out var directStartHour))
            {
                return currentHour >= directStartHour;
            }

            foreach (var configuredCourier in _wzGenerationStartHours)
            {
                if (normalizedCourier.Contains(configuredCourier.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return currentHour >= configuredCourier.Value;
                }
            }

            return currentHour >= _fallbackStartHour;
        }
    }
}