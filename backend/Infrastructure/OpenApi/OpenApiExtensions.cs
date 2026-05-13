using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

namespace Backend.Infrastructure.OpenApi;

public static class OpenApiJwtExtensions
{
    public static IServiceCollection AddOpenApiWithJwt(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<AuthorizeOperationTransformer>();
        });
        return services;
    }

    public static IEndpointRouteBuilder MapScalarWithJwt(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpenApi();
        endpoints.MapScalarApiReference(options =>
        {
            options.AddPreferredSecuritySchemes("bearerAuth");
        });
        return endpoints;
    }
}
