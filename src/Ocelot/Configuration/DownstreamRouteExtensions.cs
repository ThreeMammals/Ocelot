using System.Text.Json;

namespace Ocelot.Configuration;

public static class DownstreamRouteExtensions
{
    private static readonly HashSet<string> TruthyValues =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "true",
            "yes",
            "on",
            "ok",
            "enable",
            "enabled",
            "1",
        };

    private static readonly HashSet<string> FalsyValues =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "false",
            "no",
            "off",
            "disable",
            "disabled",
            "0",
        };

    /// <summary>
    /// The known numeric types
    /// </summary>
    private static readonly HashSet<Type> NumericTypes = new()
    {
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal),
    };

    public static T GetMetadata<T>(this DownstreamRoute downstreamRoute, string key, T defaultValue = default,
        JsonSerializerOptions jsonSerializerOptions = null)
    {
        var metadata = downstreamRoute?.MetadataOptions.Metadata;

        if (metadata == null || !metadata.TryGetValue(key, out var metadataValue))
        {
            return defaultValue;
        }

        // if the value is null, return the default value of the target type
        if (metadataValue == null)
        {
            return default;
        }

        return (T)ConvertTo(typeof(T), metadataValue, downstreamRoute.MetadataOptions,
            jsonSerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static object ConvertTo(Type targetType, string value, MetadataOptions metadataOptions,
        JsonSerializerOptions jsonSerializerOptions)
    {
        if (targetType == typeof(string))
        {
            return value;
        }

        if (targetType == typeof(bool))
        {
            return TruthyValues.Contains(value.Trim());
        }

        if (targetType == typeof(bool?))
        {
            if (TruthyValues.Contains(value.Trim()))
            {
                return true;
            }

            if (FalsyValues.Contains(value.Trim()))
            {
                return false;
            }

            return null;
        }

        if (targetType == typeof(string[]))
        {
            if (value == null)
            {
                return Array.Empty<string>();
            }

            return value.Split(metadataOptions.Separators, metadataOptions.StringSplitOption)
                .Select(s => s.Trim(metadataOptions.TrimChars))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }

        return NumericTypes.Contains(targetType)
            ? Convert.ChangeType(value, targetType, metadataOptions.CurrentCulture)
            : JsonSerializer.Deserialize(value, targetType, jsonSerializerOptions);
    }
}
