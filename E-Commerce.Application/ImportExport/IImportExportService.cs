using E_Commerce.Application.ImportExport.Models;

namespace E_Commerce.Application.ImportExport;

public interface IImportExportService
{
    Task<ImportProductsResult> ImportProductsAsync(
        Guid createdByUserId,
        Stream csvStream,
        CancellationToken cancellationToken = default);

    Task<ExportCsvResult> ExportProductsAsync(CancellationToken cancellationToken = default);

    Task<ExportCsvResult> ExportProductsSampleAsync(CancellationToken cancellationToken = default);
}
