using InvoiceGeneratorFromWZ.Contracts.Models;

namespace InvoiceGeneratorFromWZ.Contracts.Repositories
{
    public interface IDocumentRespository
    {
        public Task<IEnumerable<WZDocument>> GetWZDocuments();
    }
}
