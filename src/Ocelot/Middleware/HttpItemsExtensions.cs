using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Request.Middleware;

namespace Ocelot.Middleware
{
    public static class HttpItemsExtensions
    {
        public static void UpsertDownstreamRequest(this IDictionary<object, object> input, DownstreamRequest downstreamRequest)
        {
            input.Upsert("DownstreamRequest", downstreamRequest);
        }

        public static void UpsertDownstreamResponse(this IDictionary<object, object> input, DownstreamResponse downstreamResponse)
        {
            input.Upsert("DownstreamResponse", downstreamResponse);
        }

        public static void UpsertDownstreamRoute(this IDictionary<object, object> input, DownstreamRoute downstreamRoute)
        {
            input.Upsert("DownstreamRoute", downstreamRoute);
        }

        public static void UpsertTemplatePlaceholderNameAndValues(this IDictionary<object, object> input, List<PlaceholderNameAndValue> tPNV)
        {
            input.Upsert("TemplatePlaceholderNameAndValues", tPNV);
        }

        public static void UpsertDownstreamRoute(this IDictionary<object, object> input, DownstreamRouteFinder.DownstreamRouteHolder downstreamRoute)
        {
            input.Upsert("DownstreamRouteHolder", downstreamRoute);
        }

        public static void UpsertErrors(this IDictionary<object, object> input, List<Error> errors)
        {
            input.Upsert("Errors", errors);
        }

        public static void SetError(this IDictionary<object, object> input, Error error)
        {
            var errors = new List<Error> { error };
            input.Upsert("Errors", errors);
        }

        public static void SetIInternalConfiguration(this IDictionary<object, object> input, IInternalConfiguration config)
        {
            input.Upsert("IInternalConfiguration", config);
        }

        public static IInternalConfiguration IInternalConfiguration(this IDictionary<object, object> input)
        {
            return input.Get<IInternalConfiguration>("IInternalConfiguration");
        }

        public static List<Error> Errors(this IDictionary<object, object> input)
        {
            var errors = input.Get<List<Error>>("Errors");
            return errors ?? new();
        }

        public static DownstreamRouteFinder.DownstreamRouteHolder
            DownstreamRouteHolder(this IDictionary<object, object> input) =>
            input.Get<DownstreamRouteFinder.DownstreamRouteHolder>("DownstreamRouteHolder");

        public static List<PlaceholderNameAndValue>
            TemplatePlaceholderNameAndValues(this IDictionary<object, object> input) =>
            input.Get<List<PlaceholderNameAndValue>>("TemplatePlaceholderNameAndValues");

        public static DownstreamRequest DownstreamRequest(this IDictionary<object, object> input) =>
            input.Get<DownstreamRequest>("DownstreamRequest");

        public static DownstreamResponse DownstreamResponse(this IDictionary<object, object> input) =>
            input.Get<DownstreamResponse>("DownstreamResponse");

        public static DownstreamRoute DownstreamRoute(this IDictionary<object, object> input) =>
            input.Get<DownstreamRoute>("DownstreamRoute");

        private static T Get<T>(this IDictionary<object, object> input, string key) =>
        input.TryGetValue(key, out var value) ? (T)value : default;

        private static void Upsert<T>(this IDictionary<object, object> input, string key, T value)
        {
            if (input.DoesntExist(key))
            {
                input.Add(key, value);
            }
            else
            {
                input.Remove(key);
                input.Add(key, value);
            }
        }

        private static bool DoesntExist(this IDictionary<object, object> input, string key) => !input.ContainsKey(key);
    }
}
