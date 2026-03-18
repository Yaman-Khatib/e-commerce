using E_Commerce_API.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace E_Commerce_API.Extensions;

public static class OpenApiServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<JwtBearerOpenApiDocumentTransformer>();
            options.AddOperationTransformer<JwtBearerAuthorizeOperationTransformer>();
        });

        return services;
    }
}

