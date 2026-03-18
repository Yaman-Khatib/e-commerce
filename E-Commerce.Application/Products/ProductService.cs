using E_Commerce.Application.Products.Models;
using E_Commerce.Application.Shared;
using E_Commerce.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace E_Commerce.Application.Products;

public sealed class ProductService(
    IApplicationDbContext dbContext,
    IMemoryCache memoryCache,
    IOptions<CacheOptions> cacheOptions) : IProductService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly IOptions<CacheOptions> _cacheOptions = cacheOptions;

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var ttlSeconds = _cacheOptions.Value.ProductsLifetimeSeconds;
        if (ttlSeconds > 0 &&
            _memoryCache.TryGetValue(ProductsCacheKeys.All, out IReadOnlyList<ProductResponse>? cached) &&
            cached is not null)
        {
            return cached;
        }

        var products = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                CreatedByUserId = p.CreatedByUserId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity
            })
            .ToListAsync(cancellationToken);

        if (ttlSeconds > 0)
        {
            _memoryCache.Set(
                ProductsCacheKeys.All,
                products,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds)
                });
        }

        return products;
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                CreatedByUserId = p.CreatedByUserId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductResponse> CreateAsync(Guid createdByUserId, CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product(
            Guid.NewGuid(),
            createdByUserId,
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity);

        await _dbContext.Products.AddAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _memoryCache.Remove(ProductsCacheKeys.All);

        return MapToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.SingleOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.SetName(request.Name);
        product.SetDescription(request.Description);
        product.SetPrice(request.Price);
        product.SetStock(request.StockQuantity);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _memoryCache.Remove(ProductsCacheKeys.All);

        return MapToResponse(product);
    }

    public async Task<bool> DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.SingleOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return false;
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _memoryCache.Remove(ProductsCacheKeys.All);

        return true;
    }

    private static ProductResponse MapToResponse(Product product) =>
        new()
        {
            Id = product.Id,
            CreatedByUserId = product.CreatedByUserId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity
        };
}

