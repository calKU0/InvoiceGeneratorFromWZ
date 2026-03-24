using InvoiceGeneratorFromWZ.Contracts.Models;
using InvoiceGeneratorFromWZ.Contracts.Repositories;
using InvoiceGeneratorFromWZ.Infrastructure.Data;
using System.Data;

namespace InvoiceGeneratorFromWZ.Infrastructure.Repositories
{
    public class DocumentRepository : IDocumentRespository
    {
        private readonly IDbExecutor _dbExecutor;
        public DocumentRepository(IDbExecutor dbExecutor)
        {
            _dbExecutor = dbExecutor;
        }
        public async Task<IEnumerable<WZDocument>> GetWZDocuments()
        {
            return await _dbExecutor.QueryAsync<WZDocument>(
                "[dbo].[WZGetAllDocuments]",
                commandType: CommandType.StoredProcedure);
        }
    }
}
