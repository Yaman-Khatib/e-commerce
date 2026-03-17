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

        await dbContext.Users.AddRangeAsync(users, cancellationToken);

        var creatorUserId = users[0].Id;

        var products = new List<Product>
        {
            new(Guid.NewGuid(), creatorUserId, "Laptop", "14-inch laptop", 999.99m, 15),
            new(Guid.NewGuid(), creatorUserId, "Mouse", "Wireless mouse", 24.99m, 120),
            new(Guid.NewGuid(), creatorUserId, "Keyboard", "Mechanical keyboard", 89.99m, 60),
            new(Guid.NewGuid(), creatorUserId, "Monitor", "27-inch monitor", 219.99m, 35),
            new(Guid.NewGuid(), creatorUserId, "Headphones", "Over-ear headphones", 59.99m, 80),
            new(Guid.NewGuid(), creatorUserId, "Webcam", "1080p webcam", 39.99m, 50),
            new(Guid.NewGuid(), creatorUserId, "USB-C Cable", "1m USB-C cable", 9.99m, 200),
            new(Guid.NewGuid(), creatorUserId, "Dock", "USB-C docking station", 129.99m, 25),
            new(Guid.NewGuid(), creatorUserId, "SSD", "1TB NVMe SSD", 109.99m, 40),
            new(Guid.NewGuid(), creatorUserId, "Chair", "Ergonomic chair", 179.99m, 10)
        };

        await dbContext.Products.AddRangeAsync(products, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static User CreateUser(string username, string email, string firstName, string lastName, string? password)
    {
        return new User(Guid.NewGuid(), firstName, lastName, username, email, password ?? "seed");
    }
}

