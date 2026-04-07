using InvoiceGeneratorFromWZ.Contracts.Models;

namespace InvoiceGeneratorFromWZ.Contracts.Services
{
    public interface IXlApiService
    {
        public int Login();
        public void Logout(int sessionId);
        public void CreateInvoice(List<WZDocument> wzList, int sessionId);
    }
}
