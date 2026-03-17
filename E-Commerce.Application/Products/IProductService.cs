using E_Commerce.Application.Products.Models;

namespace E_Commerce.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ProductResponse?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<ProductResponse> CreateAsync(Guid createdByUserId, CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductResponse?> UpdateAsync(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid productId, CancellationToken cancellationToken = default);
}

