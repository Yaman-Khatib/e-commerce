using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace E_Commerce_API.OpenApi;

internal sealed class JwtBearerOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    internal const string SecuritySchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

        document.Components.SecuritySchemes[SecuritySchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "JWT Authorization header using the Bearer scheme."
        };

        return Task.CompletedTask;
    }
}

