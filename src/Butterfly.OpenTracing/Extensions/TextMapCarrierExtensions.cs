using System;

namespace Butterfly.OpenTracing
{
    public static class TextMapCarrierExtensions
    {
        public static string GetTraceId(this ITextMapCarrier carrier)
        {
            return carrier[TextMapCarrierHelpers.prefix_traceId];
        }

        public static string GetSpanId(this ITextMapCarrier carrier)
        {
            return carrier[TextMapCarrierHelpers.prefix_spanId];
        }

        public static bool GetSampled(this ITextMapCarrier carrier)
        {
            var prefix_sampled = carrier[TextMapCarrierHelpers.prefix_sampled];
            bool.TryParse(prefix_sampled, out var sampled);
            return sampled;
        }

        public static Baggage GetBaggage(this ITextMapCarrier carrier)
        {
            var baggage = new Baggage();

            foreach (var item in carrier)
            {
                if (item.Key.StartsWith(TextMapCarrierHelpers.prefix, StringComparison.Ordinal))
                {
                    var key = item.Key.Substring(TextMapCarrierHelpers.prefix.Length);
                    baggage[key] = item.Value;
                }
            }

            return baggage;
        }
    }
}