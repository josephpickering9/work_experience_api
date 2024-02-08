using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Work_Experience_Search.Filters;

public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formParameters = context.MethodInfo.GetParameters()
            .Where(p => p.GetCustomAttributes(typeof(FromFormAttribute), false).Any() ||
                        IsFileUploadType(p.ParameterType));

        if (!formParameters.Any())
            return;

        operation.RequestBody.Content.Clear();
        var schema = new OpenApiSchema { Type = "object", Properties = new Dictionary<string, OpenApiSchema>() };

        foreach (var parameter in formParameters)
            ProcessParameter(schema, parameter.ParameterType, parameter.Name, context);

        operation.RequestBody.Content.Add("multipart/form-data", new OpenApiMediaType
        {
            Schema = schema
        });
    }

    private void ProcessParameter(OpenApiSchema schema, Type type, string name, OperationFilterContext context,
        string parentName = null)
    {
        var fullName = string.IsNullOrEmpty(parentName) ? name : $"{parentName}.{name}";

        if (IsFileUploadType(type))
            AddFileSchema(schema, fullName, type);
        else if (type.IsClass && type != typeof(string))
            foreach (var property in type.GetProperties())
                ProcessParameter(schema, property.PropertyType, property.Name, context, fullName);
    }

    private static bool IsFileUploadType(Type type)
    {
        return type == typeof(IFormFile)
               || type == typeof(IFormFileCollection)
               || typeof(IEnumerable<IFormFile>).IsAssignableFrom(type);
    }

    private static void AddFileSchema(OpenApiSchema schema, string name, Type type)
    {
        if (type == typeof(IFormFile) || type == typeof(IFormFileCollection))
            schema.Properties[name] = new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            };
        else if (typeof(IEnumerable<IFormFile>).IsAssignableFrom(type))
            schema.Properties[name] = new OpenApiSchema
            {
                Type = "array",
                Items = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                }
            };
    }
}
