using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Work_Experience_Search;

using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Reflection;

public class EnumDescriptionSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            List<string> enumDescriptions = new List<string>();
            foreach (object? enumValue in Enum.GetValues(context.Type))
            {
                var memberInfo = context.Type.GetMember(enumValue.ToString()).FirstOrDefault();
                var descriptionAttribute = memberInfo?.GetCustomAttribute<DescriptionAttribute>();
                enumDescriptions.Add(descriptionAttribute?.Description ?? enumValue.ToString());
            }

            schema.Enum.Clear();
            foreach (string enumDescription in enumDescriptions)
            {
                schema.Enum.Add(new OpenApiString(enumDescription));
            }
            schema.Type = "string";
        }
    }
}