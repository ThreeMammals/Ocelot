using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace Ocelot.Configuration
{
    public static class DownstreamRouteExtensions
    {
        public static string GetMetadataValue(this DownstreamRoute downstreamRoute,
                                              string key,
                                              string defaultValue = null)
        {
            var metadata = downstreamRoute?.Metadata;

            if (metadata == null)
            {
                return defaultValue;
            }

            if (!metadata.TryGetValue(key, out string value))
            {
                return defaultValue;
            }

            return value;
        }

#if NET7_0_OR_GREATER
        public static T GetMetadataNumber<T>(this DownstreamRoute downstreamRoute,
                                             string key,
                                             T defaultValue = default,
                                             NumberStyles numberStyles = NumberStyles.Any,
                                             CultureInfo cultureInfo = null)
            where T : INumberBase<T>
        {
            var metadataValue = downstreamRoute.GetMetadataValue(key);
            if (metadataValue == null)
            {
                return defaultValue;
            }

            IFormatProvider formatProvider = cultureInfo ?? CultureInfo.CurrentCulture;
            return T.Parse(metadataValue, numberStyles, formatProvider);
        }
#endif

        public static string[] GetMetadataValues(this DownstreamRoute downstreamRoute,
                                                 string key,
                                                 string separator = ",",
                                                 StringSplitOptions stringSplitOptions = StringSplitOptions.RemoveEmptyEntries,
                                                 string trimChars = " ")
        {
            var metadataValue = downstreamRoute.GetMetadataValue(key);
            if (metadataValue == null)
            {
                return Array.Empty<string>();
            }

            var strings = metadataValue.Split(separator, stringSplitOptions);
            char[] trimCharsArray = trimChars.ToCharArray();

            for (var i = 0; i < strings.Length; i++)
            {
                strings[i] = strings[i].Trim(trimCharsArray);
            }

            return strings.Where(x => x.Length > 0).ToArray();
        }

        public static T GetMetadataFromJson<T>(this DownstreamRoute downstreamRoute,
                                               string key,
                                               T defaultValue = default,
                                               JsonSerializerOptions jsonSerializerOptions = null)
        {
            var metadataValue = downstreamRoute.GetMetadataValue(key);
            if (metadataValue == null)
            {
                return defaultValue;
            }

            return JsonSerializer.Deserialize<T>(metadataValue, jsonSerializerOptions);
        }

        public static bool IsMetadataValueTruthy(this DownstreamRoute downstreamRoute, string key)
        {
            var metadataValue = downstreamRoute.GetMetadataValue(key);
            if (metadataValue == null)
            {
                return false;
            }

            var trimmedValue = metadataValue.Trim().ToLower();
            return trimmedValue == "true" ||
                   trimmedValue == "yes" ||
                   trimmedValue == "on" ||
                   trimmedValue == "ok" ||
                   trimmedValue == "enable" ||
                   trimmedValue == "enabled";
        }
    }
}
