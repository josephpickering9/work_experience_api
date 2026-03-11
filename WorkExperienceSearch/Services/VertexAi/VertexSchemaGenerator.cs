using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services.VertexAi;

[AttributeUsage(AttributeTargets.Property)]
public sealed class VertexKeyPropertyAttribute(string mapping) : Attribute
{
    public string Mapping { get; } = mapping;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class VertexFieldAttribute(bool searchable = false, bool indexable = false, bool retrievable = false) : Attribute
{
    public bool Searchable { get; } = searchable;
    public bool Indexable { get; } = indexable;
    public bool Retrievable { get; } = retrievable;
}

/// <summary>
/// Lightweight JSON Schema generator based on DTO reflection. Avoids manual string maintenance.
/// </summary>
public static class VertexSchemaGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private const string Draft202012 = "https://json-schema.org/draft/2020-12/schema";

    public static string GenerateSchema<T>() => JsonSerializer.Serialize(BuildSchema(typeof(T), isRoot: true), SerializerOptions);

    private static object BuildSchema(Type type, bool isRoot = false)
    {
        if (TryGetPrimitiveSchema(type, out var primitive)) return primitive!;

        if (IsEnumerable(type, out var elementType))
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "array",
                ["items"] = BuildSchema(elementType)
            };
        }

        var properties = new Dictionary<string, object?>();
        var required = new List<string>();
        var nullabilityContext = new NullabilityInfoContext();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetMethod is null) continue;

            var propSchema = BuildSchema(prop.PropertyType);
            if (propSchema is Dictionary<string, object?> dict)
            {
                var keyMapping = prop.GetCustomAttribute<VertexKeyPropertyAttribute>();
                if (keyMapping != null)
                    dict["keyPropertyMapping"] = keyMapping.Mapping;

                var field = prop.GetCustomAttribute<VertexFieldAttribute>();
                var isKeyProperty = keyMapping != null;
                if (field != null)
                {
                    // Annotations on array types must go on items, not the array itself
                    var annotationTarget = dict.TryGetValue("type", out var t) && t is "array" && dict["items"] is Dictionary<string, object?> items
                        ? items
                        : dict;

                    // Key property fields cannot have searchable or indexable annotations
                    if (field.Searchable && !isKeyProperty) annotationTarget["searchable"] = true;
                    if (field.Indexable && !isKeyProperty) annotationTarget["indexable"] = true;
                    if (field.Retrievable) annotationTarget["retrievable"] = true;
                }
            }

            properties[prop.Name] = propSchema;

            var nullability = nullabilityContext.Create(prop);
            var isNullable = IsNullable(prop.PropertyType, nullability);
            if (!isNullable) required.Add(prop.Name);
        }

        var obj = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = properties
        };
        if (isRoot)
        {
            obj["$schema"] = Draft202012;
        }
        if (required.Count > 0) obj["required"] = required;

        return obj;
    }

    private static bool TryGetPrimitiveSchema(Type type, out object? schema)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(string) || underlying == typeof(Guid) || typeof(IId).IsAssignableFrom(underlying))
        {
            schema = new Dictionary<string, object?> { ["type"] = "string" };
            return true;
        }

        if (underlying == typeof(DateOnly) || underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset))
        {
            schema = new Dictionary<string, object?> { ["type"] = "datetime" };
            return true;
        }

        if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(short))
        {
            schema = new Dictionary<string, object?> { ["type"] = "integer" };
            return true;
        }

        if (underlying == typeof(double) || underlying == typeof(float) || underlying == typeof(decimal))
        {
            schema = new Dictionary<string, object?> { ["type"] = "number" };
            return true;
        }

        if (underlying == typeof(bool))
        {
            schema = new Dictionary<string, object?> { ["type"] = "boolean" };
            return true;
        }

        if (underlying.IsEnum)
        {
            schema = new Dictionary<string, object?>
            {
                ["type"] = "string",
                ["enum"] = Enum.GetNames(underlying)
            };
            return true;
        }

        schema = null;
        return false;
    }

    private static bool IsEnumerable(Type type, out Type elementType)
    {
        if (type == typeof(string))
        {
            elementType = type;
            return false;
        }

        var enumerableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface != null)
        {
            elementType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        elementType = type;
        return false;
    }

    private static bool IsNullable(Type type, NullabilityInfo nullabilityInfo)
    {
        if (Nullable.GetUnderlyingType(type) != null) return true;

        return nullabilityInfo.ReadState == NullabilityState.Nullable || nullabilityInfo.WriteState == NullabilityState.Nullable;
    }
}
