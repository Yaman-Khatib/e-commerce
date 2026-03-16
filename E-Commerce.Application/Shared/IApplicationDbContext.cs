using E_Commerce.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Application.Shared;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

