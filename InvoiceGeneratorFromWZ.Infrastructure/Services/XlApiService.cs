using cdn_api;
using InvoiceGeneratorFromWZ.Contracts.Data.Enums;
using InvoiceGeneratorFromWZ.Contracts.Models;
using InvoiceGeneratorFromWZ.Contracts.Services;
using InvoiceGeneratorFromWZ.Contracts.Settings;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;

namespace InvoiceGeneratorFromWZ.Infrastructure.Services
{
    public class XlApiService : IXlApiService
    {
        [DllImport("ClaRUN.dll")]
        public static extern void AttachThreadToClarion(int _flag);

        private readonly XlApiSettings _settings;

        private int _sessionId;

        public XlApiService(IOptions<XlApiSettings> settings)
        {
            _settings = settings.Value;
        }

        public void Login()
        {
            AttachThreadToClarion(1);

            CheckApiVersionCompatibility(_settings.ApiVersion);

            XLLoginInfo_20251 xLLoginInfo = new XLLoginInfo_20251
            {
                Wersja = _settings.ApiVersion,
                ProgramID = _settings.ProgramName,
                Baza = _settings.Database,
                OpeIdent = _settings.Username,
                OpeHaslo = _settings.Password,
                TrybWsadowy = 1
            };

            int result = cdn_api.cdn_api.XLLogin(xLLoginInfo, ref _sessionId);

            if (result != 0)
            {
                throw new Exception($"Login error. Code: {result}");
            }
        }

        public void Logout()
        {
            XLLogoutInfo_20251 xLLogoutInfo = new XLLogoutInfo_20251
            {
                Wersja = _settings.ApiVersion,
            };

            int result = cdn_api.cdn_api.XLLogout(_sessionId);

            if (result != 0)
            {
                throw new Exception($"Logout error. Code: {result}");
            }
        }

        public void CreateInvoice(List<WZDocument> wzList)
        {
            try
            {
                int documentId = 0;
                var wzHeader = wzList.First();

                ManageTransaction(0);
                XLDokumentNagInfo_20251 document = new()
                {
                    Wersja = _settings.ApiVersion,

                    Spinacz = 1,
                    Typ = wzHeader.WZType == 2001 ? 2033 : (wzHeader.WZType == 2005 ? 2037 : 0),
                    Forma = wzHeader.PaymentNumber,
                    Termin = wzHeader.PaymentDueDate,
                    Opis = string.Join(" ", wzList.DistinctBy(d => d.Description).Select(d => d.Description)).Trim(),

                    Akronim = wzHeader.ClientAcronym,
                    KnDTyp = wzHeader.ClientType,
                    KnDFirma = wzHeader.ClientCompany,
                    KnDNumer = wzHeader.ClientId,
                    KnDLp = wzHeader.ClientNo,

                    AdwTyp = wzHeader.AddressType,
                    AdwFirma = wzHeader.AddressCompany,
                    AdwNumer = wzHeader.AddressId,
                    AdwLp = wzHeader.AddressNo,
                };

                int result = cdn_api.cdn_api.XLNowyDokument(_sessionId, ref documentId, document);
                if (result != 0)
                    throw new Exception($"Error attempting to create invoice header: {CheckError(result, XlApiFunctionCode.NowyDokument)}");

                foreach (var wz in wzList)
                {
                    XLSpiInfo_20251 documentPosition = new()
                    {
                        Wersja = _settings.ApiVersion,

                        TrNTyp = wz.WZType,
                        TrNFirma = wz.WZCompany,
                        TrNNumer = wz.WZId,
                        TrNLp = wz.WZNo,
                    };

                    result = cdn_api.cdn_api.XLDodajDoSpinacza(documentId, documentPosition);
                    if (result != 0)
                        throw new Exception($"Error attempting to create invoice position: {CheckError(result, XlApiFunctionCode.DodajDoSpinacza)}");
                }

                XLZamkniecieDokumentuInfo_20251 closeDocument = new()
                {
                    Wersja = _settings.ApiVersion,
                    Tryb = 0
                };

                result = cdn_api.cdn_api.XLZamknijDokument(documentId, closeDocument);
                if (result != 0)
                    throw new Exception($"Error attempting to close binder: {CheckError(result, XlApiFunctionCode.ZamknijDokument)}");

                ManageTransaction(1);
            }
            catch
            {
                try
                {
                    ManageTransaction(2);
                }
                catch { }

                throw;
            }
        }

        private string ManageTransaction(int type, string token = "")
        {
            XLTransakcjaInfo_20251 xLTransakcja = new XLTransakcjaInfo_20251
            {
                Wersja = _settings.ApiVersion,
                Tryb = type
            };
            if (string.IsNullOrEmpty(token) && type != 0)
            {
                xLTransakcja.Token = token;
            }
            int result = cdn_api.cdn_api.XLTransakcja(_sessionId, xLTransakcja);

            if (result != 1)
            {
                throw new Exception($"Transaction error. Code: {result}");
            }

            return xLTransakcja.Token;
        }

        private string CheckError(int errorCode, XlApiFunctionCode functionCode)
        {
            XLKomunikatInfo_20251 xLKomunikat = new XLKomunikatInfo_20251
            {
                Wersja = _settings.ApiVersion,
                Funkcja = (int)functionCode,
                Blad = errorCode,
                Tryb = 0
            };
            int result = cdn_api.cdn_api.XLOpisBledu(xLKomunikat);

            if (result == 0)
                return xLKomunikat.OpisBledu;
            else
                return $"Error attempting to check error. Code: {result}";
        }

        private void CheckApiVersionCompatibility(Int32 APIVersion)
        {
            if (cdn_api.cdn_api.XLSprawdzWersje(ref APIVersion) != 0)
            {
                throw new Exception("The current API version is not supported by the current XL version");
            }
        }
    }
}
