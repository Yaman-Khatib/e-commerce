using System.Globalization;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using E_Commerce.Application.ImportExport.Models;
using E_Commerce.Application.Products;
using E_Commerce.Application.Shared;
using E_Commerce.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace E_Commerce.Application.ImportExport;

public sealed class ImportExportService(IApplicationDbContext dbContext, IMemoryCache memoryCache) : IImportExportService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IMemoryCache _memoryCache = memoryCache;

    public async Task<ImportProductsResult> ImportProductsAsync(
        Guid createdByUserId,
        Stream csvStream,
        CancellationToken cancellationToken = default)
    {
        if (createdByUserId == Guid.Empty)
        {
            return new ImportProductsResult(
                Succeeded: false,
                ImportedCount: 0,
                Errors: new[] { new ImportRowError(0, "CreatedByUserId must be a non-empty GUID.", "CreatedByUserId") });
        }

        if (csvStream is null)
        {
            return new ImportProductsResult(
                Succeeded: false,
                ImportedCount: 0,
                Errors: new[] { new ImportRowError(0, "CSV stream is required.", "File") });
        }

        var errors = new List<ImportRowError>();
        var productsToInsert = new List<Product>();

        Stream streamToRead = csvStream;
        MemoryStream? ownedCopy = null;
        try
        {
            if (!streamToRead.CanSeek)
            {
                ownedCopy = new MemoryStream();
                await streamToRead.CopyToAsync(ownedCopy, cancellationToken);
                ownedCopy.Position = 0;
                streamToRead = ownedCopy;
            }
            else if (streamToRead.Position != 0)
            {
                streamToRead.Position = 0;
            }

            using (var headerReader = new StreamReader(streamToRead, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
            {
                var headerLine = await headerReader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    return new ImportProductsResult(
                        Succeeded: false,
                        ImportedCount: 0,
                        Errors: new[] { new ImportRowError(0, "CSV file is empty or missing a header row.", "Header") });
                }

                var delimiter = DetectDelimiter(headerLine);
                streamToRead.Position = 0;

                using var reader = new StreamReader(streamToRead, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = delimiter.ToString(),
                    TrimOptions = TrimOptions.Trim,
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    DetectColumnCountChanges = false,
                    BadDataFound = args =>
                    {
                        var rowNumber = args.Context?.Parser?.RawRow ?? 0;
                        errors.Add(new ImportRowError(rowNumber, "Malformed CSV data.", "Row"));
                    }
                };

                using var csv = new CsvReader(reader, config);

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!await csv.ReadAsync())
                    {
                        return new ImportProductsResult(
                            Succeeded: false,
                            ImportedCount: 0,
                            Errors: new[] { new ImportRowError(0, "CSV file is missing a header row.", "Header") });
                    }

                    csv.ReadHeader();
                    if (csv.HeaderRecord is null || csv.HeaderRecord.Length == 0)
                    {
                        return new ImportProductsResult(
                            Succeeded: false,
                            ImportedCount: 0,
                            Errors: new[] { new ImportRowError(0, "CSV file is missing a header row.", "Header") });
                    }
                }
                catch (Exception ex)
                {
                    return new ImportProductsResult(
                        Succeeded: false,
                        ImportedCount: 0,
                        Errors: new[] { new ImportRowError(0, $"Failed to read CSV header: {ex.Message}", "Header") });
                }

                var headerMap = BuildHeaderMap(csv.HeaderRecord);
                var missing = GetMissingRequiredHeaders(headerMap);
                if (missing.Count > 0)
                {
                    return new ImportProductsResult(
                        Succeeded: false,
                        ImportedCount: 0,
                        Errors: new[]
                        {
                            new ImportRowError(0, $"Missing required headers: {string.Join(", ", missing)}", "Header")
                        });
                }

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!await csv.ReadAsync())
                    {
                        break;
                    }

                    var rowNumber = csv.Context.Parser.RawRow;

                    string? name = null;
                    string? description = null;
                    decimal? price = null;
                    int? stockQuantity = null;

                    try
                    {
                        name = GetField(csv, headerMap, "name");
                        description = GetField(csv, headerMap, "description");

                        var priceRaw = GetField(csv, headerMap, "price");
                        price = TryParseDecimal(priceRaw, delimiter, out var parsedPrice) ? parsedPrice : null;

                        var stockRaw = GetField(csv, headerMap, "stockquantity");
                        stockQuantity = int.TryParse(stockRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedStock)
                            ? parsedStock
                            : (int?)null;
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new ImportRowError(rowNumber, $"Failed to read row: {ex.Message}", "Row"));
                        continue;
                    }

                    var nameTrimmed = name?.Trim();
                    if (string.IsNullOrWhiteSpace(nameTrimmed))
                    {
                        errors.Add(new ImportRowError(rowNumber, "Name is required.", "Name"));
                    }

                    if (price is null)
                    {
                        errors.Add(new ImportRowError(rowNumber, "Price is required and must be a valid number.", "Price"));
                    }
                    else if (price.Value < 0)
                    {
                        errors.Add(new ImportRowError(rowNumber, "Price cannot be negative.", "Price"));
                    }

                    if (stockQuantity is null)
                    {
                        errors.Add(new ImportRowError(rowNumber, "StockQuantity is required and must be a valid integer.", "StockQuantity"));
                    }
                    else if (stockQuantity.Value < 0)
                    {
                        errors.Add(new ImportRowError(rowNumber, "StockQuantity cannot be negative.", "StockQuantity"));
                    }

                    if (errors.Any(e => e.RowNumber == rowNumber))
                    {
                        continue;
                    }

                    try
                    {
                        var product = new Product(
                            Guid.NewGuid(),
                            createdByUserId,
                            nameTrimmed!,
                            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                            price!.Value,
                            stockQuantity!.Value);

                        productsToInsert.Add(product);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new ImportRowError(rowNumber, ex.Message, "Row"));
                    }
                }
            }
        }
        finally
        {
            if (ownedCopy is not null)
            {
                await ownedCopy.DisposeAsync();
            }
        }

        if (errors.Count > 0)
        {
            return new ImportProductsResult(
                Succeeded: false,
                ImportedCount: 0,
                Errors: errors
                    .OrderBy(e => e.RowNumber)
                    .ThenBy(e => e.Field)
                    .ToList());
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _dbContext.Products.AddRangeAsync(productsToInsert, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        _memoryCache.Remove(ProductsCacheKeys.All);

        return new ImportProductsResult(
            Succeeded: true,
            ImportedCount: productsToInsert.Count,
            Errors: Array.Empty<ImportRowError>());
    }

    public async Task<ExportCsvResult> ExportProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Name,
                p.Description,
                p.Price,
                p.StockQuantity
            })
            .ToListAsync(cancellationToken);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            NewLine = Environment.NewLine
        };

        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using (var csv = new CsvWriter(stringWriter, config))
        {
            csv.WriteField("Name");
            csv.WriteField("Description");
            csv.WriteField("Price");
            csv.WriteField("StockQuantity");
            await csv.NextRecordAsync();

            foreach (var product in products)
            {
                csv.WriteField(product.Name);
                csv.WriteField(product.Description);
                csv.WriteField(product.Price.ToString(CultureInfo.InvariantCulture));
                csv.WriteField(product.StockQuantity);
                await csv.NextRecordAsync();
            }
        }

        var fileName = $"products_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_utc.csv";
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(stringWriter.ToString());

        return new ExportCsvResult(fileName, "text/csv", bytes);
    }

    public Task<ExportCsvResult> ExportProductsSampleAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            NewLine = Environment.NewLine
        };

        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using (var csv = new CsvWriter(stringWriter, config))
        {
            csv.WriteField("Name");
            csv.WriteField("Description");
            csv.WriteField("Price");
            csv.WriteField("StockQuantity");
            csv.NextRecord();
        }

        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(stringWriter.ToString());
        return Task.FromResult(new ExportCsvResult("products_sample.csv", "text/csv", bytes));
    }

    private static char DetectDelimiter(string headerLine)
    {
        var commaCount = headerLine.Count(c => c == ',');
        var semicolonCount = headerLine.Count(c => c == ';');

        if (semicolonCount == 0 && commaCount == 0)
        {
            return ',';
        }

        return semicolonCount > commaCount ? ';' : ',';
    }

    private static Dictionary<string, string> BuildHeaderMap(string[] headers)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            var normalized = NormalizeHeader(header);
            if (!string.IsNullOrWhiteSpace(normalized) && !map.ContainsKey(normalized))
            {
                map[normalized] = header;
            }
        }

        return map;
    }

    private static List<string> GetMissingRequiredHeaders(Dictionary<string, string> headerMap)
    {
        var required = new[] { "name", "description", "price", "stockquantity" };
        return required.Where(r => !headerMap.ContainsKey(r)).ToList();
    }

    private static string GetField(CsvReader csv, Dictionary<string, string> headerMap, string normalizedHeader)
    {
        var header = headerMap[normalizedHeader];
        return csv.GetField(header) ?? string.Empty;
    }

    private static string NormalizeHeader(string header)
    {
        var chars = header
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();

        return new string(chars);
    }

    private static bool TryParseDecimal(string? raw, char delimiter, out decimal value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        raw = raw.Trim();
        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        if (delimiter == ';')
        {
            var tr = CultureInfo.GetCultureInfo("tr-TR");
            if (decimal.TryParse(raw, NumberStyles.Number, tr, out value))
            {
                return true;
            }
        }

        return false;
    }
}
