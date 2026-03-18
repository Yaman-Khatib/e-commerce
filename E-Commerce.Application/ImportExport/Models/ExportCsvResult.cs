namespace E_Commerce.Application.ImportExport.Models;

public sealed record ExportCsvResult(string FileName, string ContentType, byte[] Bytes);

