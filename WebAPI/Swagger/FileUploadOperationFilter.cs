using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebAPI.Swagger
{
    public sealed class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Only touch actions that explicitly consume multipart/form-data
            var isMultipart = context.MethodInfo.GetCustomAttributes(true)
                .OfType<ConsumesAttribute>()
                .Any(a => a.ContentTypes.Contains("multipart/form-data"));

            if (!isMultipart) return;

            var parameters = context.MethodInfo.GetParameters();

            // If there's a complex [FromForm] model (e.g., SubmissionUploadForm),
            // let Swashbuckle generate its fields (CourseId, File, ...).
            var hasComplexFromForm = parameters.Any(p =>
                p.GetCustomAttributes(typeof(FromFormAttribute), inherit: true).Any() &&
                p.ParameterType != typeof(IFormFile) &&
                !typeof(IEnumerable<IFormFile>).IsAssignableFrom(p.ParameterType) &&
                p.ParameterType != typeof(string) &&
                !p.ParameterType.IsPrimitive);

            if (hasComplexFromForm) return;

            // Bare file(s) parameter?
            var hasBareFile = parameters.Any(p =>
                p.ParameterType == typeof(IFormFile) ||
                typeof(IEnumerable<IFormFile>).IsAssignableFrom(p.ParameterType));

            if (!hasBareFile) return;

            // Force a clean single file field
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties =
                            {
                                ["file"] = new OpenApiSchema { Type = "string", Format = "binary" }
                            },
                            Required = new HashSet<string> { "file" }
                        }
                    }
                }
            };
        }
    }
}
