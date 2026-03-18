namespace E_Commerce.Application.ImportExport.Models;

public sealed record ImportProductsResult(
    bool Succeeded,
    int ImportedCount,
    IReadOnlyList<ImportRowError> Errors);

