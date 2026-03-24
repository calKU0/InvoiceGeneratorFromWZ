using InvoiceGeneratorFromWZ.Contracts.Repositories;
using InvoiceGeneratorFromWZ.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace InvoiceGeneratorFromWZ.Service.Services
{
    public class InvoiceProcessingService : IInvoiceProcessingService
    {
        private readonly IDocumentRespository _documentRepository;
        private readonly IXlApiService _xlApiService;
        private readonly ILogger<InvoiceProcessingService> _logger;

        public InvoiceProcessingService(IDocumentRespository documentRepository, IXlApiService xlApiService, ILogger<InvoiceProcessingService> logger)
        {
            _documentRepository = documentRepository;
            _xlApiService = xlApiService;
            _logger = logger;
        }

        public async Task ProcessInvoicesAsync()
        {
            var documents = await _documentRepository.GetWZDocuments();
            var today = DateTime.Now.Day;

            // Filter out using the new encapsulated logic on the model
            var toInvoice = documents.Where(d => d.ShouldInvoiceToday(today)).ToList();

            if (!toInvoice.Any())
            {
                _logger.LogInformation("No documents to make invoice for day {Day}.", today);
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

            try
            {
                _xlApiService.Login();

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
            finally
            {
                try
                {
                    _xlApiService.Logout();
                }
                catch
                {
                }
            }
        }
    }
}