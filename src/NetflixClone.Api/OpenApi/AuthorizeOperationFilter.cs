using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NetflixClone.Api.OpenApi;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.IsDefined(typeof(NetflixClone.Api.Security.RequireProfileAccessAttribute), true) ||
            context.MethodInfo.DeclaringType?.IsDefined(typeof(NetflixClone.Api.Security.RequireProfileAccessAttribute), true) == true)
        {
            operation.Parameters ??= new List<IOpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = NetflixClone.Api.Security.ProfileAccessFilter.HeaderName,
                In = ParameterLocation.Header,
                Required = false,
                Description = "Unlock token returned by POST /api/profiles/{profileId}/unlock. Required when the profile has a PIN.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            });
        }
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
        {
            return;
        }

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = new List<string>()
            }
        };
    }
}
