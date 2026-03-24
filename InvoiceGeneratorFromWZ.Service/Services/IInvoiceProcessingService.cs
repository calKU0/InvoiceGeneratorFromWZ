using InvoiceGeneratorFromWZ.Contracts.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvoiceGeneratorFromWZ.Service.Services
{
    public interface IInvoiceProcessingService
    {
        Task ProcessInvoicesAsync();
    }
}