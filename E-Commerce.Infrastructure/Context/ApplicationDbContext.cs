using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using E_Commerce.Application.Shared;
using Microsoft.EntityFrameworkCore.Infrastructure;
using E_Commerce.Domain.Products;

namespace E_Commerce.Infrastructure.Context
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
    {
        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
