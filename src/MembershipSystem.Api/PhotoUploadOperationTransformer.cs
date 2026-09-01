using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MembershipSystem.Api;

/// <summary>
/// AddOpenApi()'s built-in generator has no special handling for
/// IFormFile — it renders it as a plain JSON object (ContentType,
/// Headers, Length, etc.) instead of a multipart file upload, so
/// Swagger UI shows a schema editor instead of a file picker. This
/// rewrites the one action that takes an IFormFile parameter
/// (MembersController.SetPhotoFromForm, routed at
/// PUT /branches/{branchId}/members/{memberId}/photo) to the correct
/// multipart/form-data + type: string, format: binary shape.
/// Matched by route + HTTP method rather than by inspecting parameter
/// descriptions, since IFormFile-bound parameters aren't reliably typed
/// as IFormFile by the time this transformer runs.
/// </summary>
public sealed class PhotoUploadOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var isPhotoUpload =
            context.Description.HttpMethod == "PUT" &&
            context.Description.RelativePath == "branches/{branchId}/members/{memberId}/photo";

        if (!isPhotoUpload)
        {
            return Task.CompletedTask;
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["file"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                Format = "binary",
                            },
                        },
                        Required = new HashSet<string> { "file" },
                    },
                },
            },
        };

        return Task.CompletedTask;
    }
}
