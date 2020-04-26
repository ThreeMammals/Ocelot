namespace Ocelot.DownstreamRouteFinder.Middleware
{
    using Ocelot.Configuration;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Errors;
    using Ocelot.Middleware;
    using Ocelot.Request.Middleware;
    using System.Collections.Generic;

    public static class HttpItemsExtensions
    {
        public static void SetDownstreamRequest(this IDictionary<object, object> input, DownstreamRequest downstreamRequest)
        {
            input.Set("DownstreamRequest", downstreamRequest);
        }

        public static void SetDownstreamResponse(this IDictionary<object, object> input, DownstreamResponse downstreamResponse)
        {
            input.Set("DownstreamResponse", downstreamResponse);
        }

        public static void SetDownstreamReRoute(this IDictionary<object, object> input, DownstreamReRoute downstreamReRoute)
        {
            input.Set("DownstreamReRoute", downstreamReRoute);
        }

        public static void SetTemplatePlaceholderNameAndValues(this IDictionary<object, object> input, List<PlaceholderNameAndValue> tPNV)
        {
            input.Set("TemplatePlaceholderNameAndValues", tPNV);
        }

        public static void SetDownstreamRoute(this IDictionary<object, object> input, DownstreamRoute downstreamRoute)
        {
            input.Set("DownstreamRoute", downstreamRoute);
        }

        public static void SetErrors(this IDictionary<object, object> input, List<Error> errors)
        {
            input.Set("Errors", errors);
        }

        public static void SetError(this IDictionary<object, object> input, Error error)
        {
            var errors = new List<Error>() { error };
            input.Set("Errors", errors);
        }

        public static void SetIInternalConfiguration(this IDictionary<object, object> input, IInternalConfiguration config)
        {
            input.Set("IInternalConfiguration", config);
        }

        public static IInternalConfiguration IInternalConfiguration(this IDictionary<object, object> input)
        {
            return input.Get<IInternalConfiguration>("IInternalConfiguration");
        }

        public static List<Error> Errors(this IDictionary<object, object> input)
        {
            var errors = input.Get<List<Error>>("Errors");
            return errors == null ? new List<Error>() : errors;
        }

        public static DownstreamRoute DownstreamRoute(this IDictionary<object, object> input)
        {
            return input.Get<DownstreamRoute>("DownstreamRoute");
        }

        public static List<PlaceholderNameAndValue> TemplatePlaceholderNameAndValues(this IDictionary<object, object> input)
        {
            return input.Get<List<PlaceholderNameAndValue>>("TemplatePlaceholderNameAndValues");
        }

        public static DownstreamRequest DownstreamRequest(this IDictionary<object, object> input)
        {
            return input.Get<DownstreamRequest>("DownstreamRequest");
        }

        public static DownstreamResponse DownstreamResponse(this IDictionary<object, object> input)
        {
            return input.Get<DownstreamResponse>("DownstreamResponse");
        }

        public static DownstreamReRoute DownstreamReRoute(this IDictionary<object, object> input)
        {
            return input.Get<DownstreamReRoute>("DownstreamReRoute");
        }

        private static T Get<T>(this IDictionary<object, object> input, string key)
        {
            if (input.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            return default(T);
        }

        private static void Set<T>(this IDictionary<object, object> input, string key, T value)
        {
            input.Add(key, value);
        }
    }
}
