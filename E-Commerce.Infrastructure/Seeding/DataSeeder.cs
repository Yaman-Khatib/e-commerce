using E_Commerce.Domain.Products;
using E_Commerce.Domain.Users;
using E_Commerce.Infrastructure.Context;
using E_Commerce.Application.Users.Security;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Infrastructure.Seeding;

public class DataSeeder(ApplicationDbContext dbContext, IPasswordHashService passwordHashService) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken) || await dbContext.Products.AnyAsync(cancellationToken))
            return;

        var hashedPassword = passwordHashService.Hash("Password");
        var users = new List<User>
        {
            CreateUser("user1", "user1@example.com", "Ahmad","Khatem", hashedPassword),
            CreateUser("user2", "user2@example.com", "Malek","Ali", hashedPassword),
            CreateUser("user3", "user3@example.com", "Amr","Haddad",hashedPassword)
        };


        var products = new List<Product>
        {
            new(Guid.NewGuid(), "Laptop", "14-inch laptop", 999.99m, 15),
            new(Guid.NewGuid(), "Mouse", "Wireless mouse", 24.99m, 120),
            new(Guid.NewGuid(), "Keyboard", "Mechanical keyboard", 89.99m, 60),
            new(Guid.NewGuid(), "Monitor", "27-inch monitor", 219.99m, 35),
            new(Guid.NewGuid(), "Headphones", "Over-ear headphones", 59.99m, 80),
            new(Guid.NewGuid(), "Webcam", "1080p webcam", 39.99m, 50),
            new(Guid.NewGuid(), "USB-C Cable", "1m USB-C cable", 9.99m, 200),
            new(Guid.NewGuid(), "Dock", "USB-C docking station", 129.99m, 25),
            new(Guid.NewGuid(), "SSD", "1TB NVMe SSD", 109.99m, 40),
            new(Guid.NewGuid(), "Chair", "Ergonomic chair", 179.99m, 10)
        };

        await dbContext.Users.AddRangeAsync(users, cancellationToken);
        await dbContext.Products.AddRangeAsync(products, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static User CreateUser(string username, string email, string firstName, string lastName, string? password)
    {
        return new User(Guid.NewGuid(), firstName, lastName, username, email, password ?? "seed");
    }
}

