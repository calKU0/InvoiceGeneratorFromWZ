# InvoiceGeneratorFromWZ

`InvoiceGeneratorFromWZ` is a `.NET 10` Windows Worker Service that automatically creates sales invoices in Comarch XL based on WZ documents retrieved from SQL Server.

## What the solution does

The service runs in the background, checks eligible WZ documents on a schedule, groups them by business rules, and creates invoice documents through the XL API.

## Solution structure

- `InvoiceGeneratorFromWZ.Service`
  - Worker host and scheduling logic.
  - Dependency injection and Serilog setup.
  - Main orchestration via `Worker` and `InvoiceProcessingService`.
- `InvoiceGeneratorFromWZ.Infrastructure`
  - SQL access with `Dapper` (`DapperDbExecutor`).
  - Repository implementation (`DocumentRepository`).
  - Comarch XL integration (`XlApiService`) using `cdn_api20251.net.dll`.
- `InvoiceGeneratorFromWZ.Contracts`
  - Shared models, interfaces, enums, and settings contracts.

## Processing flow

1. `Worker` runs continuously as a hosted background service.
2. At the configured hour (once per day), it triggers `IInvoiceProcessingService.ProcessInvoicesAsync()`.
3. `InvoiceProcessingService` loads WZ documents from `IDocumentRespository`.
4. Documents are filtered by `WZDocument.ShouldInvoiceToday(day)`.
5. Remaining items are grouped by:
   - `ClientAcronym`
   - `Courier`
   - `PaymentNumber`
   - `PaymentDueDate`
   - `AddressId`
   - `ClientId`
6. For each group, `IXlApiService`:
   - Logs in to XL,
   - Starts a transaction,
   - Creates an invoice header,
   - Attaches WZ documents to the binder,
   - Closes the invoice,
   - Commits the transaction (or rolls back on failure),
   - Logs out.

## Key domain model

- `WZDocument`
  - Represents source WZ data required for invoice generation.
  - Includes `DaysWhenMakeInvoice` and `ShouldInvoiceToday(int day)` for billing-day selection.

## Logging and reliability

- Logging is handled by `Serilog`.
- Logs are written to:
  - Console
  - Daily rolling files in `logs` under the service base directory
- Group-level failures are logged and do not stop processing of other groups.
- XL login/logout is protected with `try/finally` in processing flow.

## Technologies used

- `.NET 10` Worker Service
- `Microsoft.Extensions.Hosting.WindowsServices`
- `Dapper`
- `Microsoft.Data.SqlClient`
- `Serilog` (`Console` + `File` sinks)
- Comarch XL API (`cdn_api20251.net.dll`)

## License

This project is **proprietary and confidential**.

It was developed for a client and is **not permitted to be shared, redistributed, or used** without explicit written permission from the owner.

See [LICENSE](LICENSE) for details.

---

© 2026-present [calKU0](https://github.com/calKU0)

