using InvoiceGeneratorFromWZ.Contracts.Data.Enums;
using System;

namespace InvoiceGeneratorFromWZ.Infrastructure.Mapping
{
    public static class InvoiceTypeMapper
    {
        public static InvoiceDocumentType MapWZTypeToInvoiceType(WzDocumentType wzType)
        {
            return wzType switch
            {
                WzDocumentType.WZ => InvoiceDocumentType.FS,
                WzDocumentType.WZE => InvoiceDocumentType.FSE,
                WzDocumentType.WZK => InvoiceDocumentType.FSK,
                WzDocumentType.WKE => InvoiceDocumentType.FKE,
                _ => throw new ArgumentException($"Unsupported WZ type: {wzType}")
            };
        }
    }
}
