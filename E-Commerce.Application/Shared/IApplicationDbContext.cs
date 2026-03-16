using E_Commerce.Domain.Products;
using E_Commerce.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Application.Shared;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

