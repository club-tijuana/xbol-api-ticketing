using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.API.Schema;

public static class AppSettingsSchemaGenerator
{
    /// <summary>
    /// Generates a JSON Schema for appsettings.json, merging custom sections
    /// with SchemaStore's built-in ASP.NET schema via allOf.
    /// </summary>
    public static JsonObject Generate()
    {
        var exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (context, node) =>
            {
                var description = context.PropertyInfo?.AttributeProvider
                    ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    .OfType<DescriptionAttribute>()
                    .FirstOrDefault()?.Description;

                if (description is not null)
                {
                    node["description"] = description;
                }

                var defaultValue = context.PropertyInfo?.AttributeProvider
                    ?.GetCustomAttributes(typeof(DefaultValueAttribute), false)
                    .OfType<DefaultValueAttribute>()
                    .FirstOrDefault()?.Value;

                if (defaultValue is not null)
                {
                    node["default"] = JsonSerializer.SerializeToNode(defaultValue);
                }

                return node;
            }
        };

        var customSchema = JsonSchemaExporter.GetJsonSchemaAsNode(
            JsonSerializerOptions.Default, typeof(AppSettingsSchema), exporterOptions);

        return new JsonObject
        {
            ["allOf"] = new JsonArray
            {
                new JsonObject
                {
                    ["$ref"] = "https://json.schemastore.org/appsettings.json"
                }
            },
            ["properties"] = customSchema["properties"]?.DeepClone(),
            ["type"] = "object"
        };
    }

    /// <summary>
    /// Generates the schema and writes it to the specified file path.
    /// </summary>
    public static void GenerateAndWrite(string outputPath)
    {
        var schema = Generate();
        var json = schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
    }

    /// <summary>
    /// Composite type representing app-specific appsettings.json sections.
    /// Add a property here for each new Options class.
    /// </summary>
    public sealed class AppSettingsSchema
    {
        [Description("Seats.io API configuration")]
        public SeatsIoOptions? SeatsIoApi { get; set; }
    }
}
