using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using E_Commerce.Application.Shared;
using Microsoft.EntityFrameworkCore.Infrastructure;
using E_Commerce.Domain.Orders;
using E_Commerce.Domain.Products;
using E_Commerce.Domain.Users;

namespace E_Commerce.Infrastructure.Context
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
    {
        public DbSet<Product> Products => Set<Product>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

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
