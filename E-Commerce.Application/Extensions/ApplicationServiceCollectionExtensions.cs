using E_Commerce.Application.Users;
using E_Commerce.Application.Users.Models;
using E_Commerce.Application.Users.Security;
using E_Commerce.Application.Shared;
using E_Commerce.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using E_Commerce.Application.Products;
using E_Commerce.Application.Orders;
using E_Commerce.Application.ImportExport;

namespace E_Commerce.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<CacheOptions>(configuration.GetSection("Cache"));
        services.AddHttpContextAccessor();
        services.AddSingleton<IPasswordHashService, BcryptPasswordHashService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IImportExportService, ImportExportService>();

        return services;
    }
}

