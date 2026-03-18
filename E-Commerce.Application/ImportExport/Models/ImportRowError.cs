namespace E_Commerce.Application.ImportExport.Models;

public sealed record ImportRowError(int RowNumber, string Message, string? Field = null);

