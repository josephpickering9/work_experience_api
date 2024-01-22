namespace Work_Experience_Search;

using System.Reflection;
using Microsoft.AspNetCore.Mvc;

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        IEnumerable<ParameterInfo> formParameters = context.MethodInfo.GetParameters()
            .Where(p => p.GetCustomAttributes(typeof(FromFormAttribute), false).Length > 0
                        || p.ParameterType == typeof(IFormFile)
                        || p.ParameterType == typeof(IFormFileCollection)
                        || p.ParameterType == typeof(List<IFormFile>));
    
        if (!formParameters.Any())
            return;
    
        operation.RequestBody.Content.Clear();
        OpenApiSchema schema = new OpenApiSchema();
    
        foreach (ParameterInfo p in formParameters)
        {
            if (p.ParameterType == typeof(IFormFile))
            {
                schema.Properties.Add(p.Name, new OpenApiSchema()
                {
                    Type = "string",
                    Format = "binary"
                });
            }
            else if (p.ParameterType == typeof(IFormCollection) || p.ParameterType == typeof(List<IFormFile>))
            {

                schema.Properties.Add(p.Name, new OpenApiSchema()
                {
                    Type = "array",
                    Items = new OpenApiSchema()
                    {
                        Type = "string",
                        Format = "binary"
                    }
                });
            }
            else
            {
                OpenApiSchema reference = context.SchemaGenerator.GenerateSchema(p.ParameterType, context.SchemaRepository);
                schema.Properties.Add(p.Name, reference);
            }
        }
    
        operation.RequestBody.Content.Add("multipart/form-data", new OpenApiMediaType()
        {
            Schema = schema
        });
    }
}
