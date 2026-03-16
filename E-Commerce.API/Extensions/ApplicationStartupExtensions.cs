using E_Commerce.Infrastructure.Context;
using E_Commerce.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce_API.Extensions;

public static class ApplicationStartupExtensions
{
    public static async Task<WebApplication> EnsureDatabaseAndSeedAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();

        var shouldMigrate = true;

        try
        {
            shouldMigrate = !await dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            shouldMigrate = true;
        }

        if (shouldMigrate)
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        await seeder.SeedAsync(cancellationToken);

        return app;
    }
}

