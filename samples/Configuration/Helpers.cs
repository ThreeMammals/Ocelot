using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Ocelot.Samples.Configuration;

public static class Helpers
{
    public static List<JProperty> OcelotCustomProperties<TFileModel>(this JObject jsonObject)
    {
        // Get all property names from the schema class
        var schemaProperties = typeof(TFileModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet();

        // Extract custom properties not in schema
        var customProperties = jsonObject.Properties()
            .Where(p => !schemaProperties.Contains(p.Name))
            .ToList();

        return customProperties;
    }
}
