using Ocelot.Configuration;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Ocelot.Metadata;

public static class DownstreamRouteExtensions
{
    /// <summary>
    /// The known truthy values
    /// </summary>
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

    /// <summary>
    /// The known falsy values
    /// </summary>
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

    /// <summary>
    /// Extension method to get metadata from a downstream route.
    /// </summary>
    /// <typeparam name="T">The metadata target type.</typeparam>
    /// <param name="downstreamRoute">The current downstream route.</param>
    /// <param name="key">The metadata key in downstream route Metadata dictionary.</param>
    /// <param name="defaultValue">The fallback value if no value found.</param>
    /// <param name="jsonSerializerOptions">Custom json serializer options if needed.</param>
    /// <returns>The parsed metadata value.</returns>
    public static T GetMetadata<T>(this DownstreamRoute downstreamRoute, string key, T defaultValue = default,
        JsonSerializerOptions jsonSerializerOptions = null)
    {
        var metadata = downstreamRoute?.MetadataOptions.Metadata;

        if (metadata == null || !metadata.TryGetValue(key, out var metadataValue) || metadataValue == null)
        {
            return defaultValue;
        }

        return (T)ConvertTo(typeof(T), metadataValue, downstreamRoute.MetadataOptions,
            jsonSerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    /// <summary>
    /// Converting a string value to the target type
    /// Some custom conversion has been for the following types:
    /// bool, bool?, string[], numeric types
    /// otherwise trying to deserialize the value using the JsonSerializer
    /// </summary>
    /// <param name="targetType">The target type.</param>
    /// <param name="value">The string value.</param>
    /// <param name="metadataOptions">The metadata options, it includes the global configuration.</param>
    /// <param name="jsonSerializerOptions">If needed, some custom json serializer options.</param>
    /// <returns>The converted string.</returns>
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
            ? ConvertToNumericType(value, targetType, metadataOptions.CurrentCulture, metadataOptions.NumberStyle)
            : JsonSerializer.Deserialize(value, targetType, jsonSerializerOptions);
    }

    /// <summary>
    /// Using reflection to invoke the Parse method of the numeric type
    /// </summary>
    /// <param name="value">The number as string.</param>
    /// <param name="targetType">The target numeric type.</param>
    /// <param name="provider">The current format provider.</param>
    /// <param name="numberStyle">The current number style configuration.</param>
    /// <returns>The parsed string as object of type targetType.</returns>
    /// <exception cref="InvalidOperationException">Exception thrown if the type doesn't contain a "Parse" method. This shouldn't happen.</exception>
    private static object ConvertToNumericType(string value, Type targetType, IFormatProvider provider,
        NumberStyles numberStyle)
    {
        MethodInfo parseMethod =
            targetType.GetMethod("Parse", new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider) }) ??
            throw new InvalidOperationException("No suitable parse method found.");

        try
        {
            // Invoke the parse method dynamically with the number style and format provider
            return parseMethod.Invoke(null, new object[] { value, numberStyle, provider });
        }
        catch (TargetInvocationException e)
        {
            // if the parse method throws an exception, rethrow the inner exception
            throw e.InnerException ?? e;
        }
    }
}
