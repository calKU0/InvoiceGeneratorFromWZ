using InvoiceGeneratorFromWZ.Contracts.Models;

namespace InvoiceGeneratorFromWZ.Contracts.Services
{
    public interface IXlApiService
    {
        public void Login();
        public void Logout();
        public void CreateInvoice(List<WZDocument> wzList);
    }
}
