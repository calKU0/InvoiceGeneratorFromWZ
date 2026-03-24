using InvoiceGeneratorFromWZ.Contracts.Data.Enums;

namespace InvoiceGeneratorFromWZ.Contracts.Models
{
    public class WZDocument
    {
        public string DocumentName { get; set; }
        public WzDocumentType WZType { get; set; }
        public int WZCompany { get; set; }
        public int WZId { get; set; }
        public int WZNo { get; set; }

        public string Courier { get; set; }
        public int PaymentNumber { get; set; }
        public int PaymentDueDate { get; set; }
        public string Description { get; set; }

        public int AddressType { get; set; }
        public int AddressCompany { get; set; }
        public int AddressId { get; set; }
        public int AddressNo { get; set; }

        public string ClientAcronym { get; set; }
        public int ClientType { get; set; }
        public int ClientCompany { get; set; }
        public int ClientId { get; set; }
        public int ClientNo { get; set; }

        public string DaysWhenMakeInvoice { get; set; }

        public bool ShouldInvoiceToday(int day) 
        {
            if (string.IsNullOrWhiteSpace(DaysWhenMakeInvoice)) return false;
            return DaysWhenMakeInvoice.Split(',').Select(s => s.Trim()).Contains(day.ToString());
        }
    }
}
