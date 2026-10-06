using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Request.Middleware;

namespace Ocelot.Middleware;

public static class HttpItemsExtensions
{
    public static void UpsertDownstreamRequest(this IDictionary<object, object> input, DownstreamRequest downstreamRequest)
    {
        input.Upsert(nameof(DownstreamRequest), downstreamRequest);
    }

    public static void UpsertDownstreamResponse(this IDictionary<object, object> input, DownstreamResponse downstreamResponse)
    {
        input.Upsert(nameof(DownstreamResponse), downstreamResponse);
    }

    public static void UpsertDownstreamRoute(this IDictionary<object, object> input, DownstreamRoute downstreamRoute)
    {
        input.Upsert(nameof(DownstreamRoute), downstreamRoute);
    }

    public static void UpsertTemplatePlaceholderNameAndValues(this IDictionary<object, object> input, List<PlaceholderNameAndValue> tPNV)
    {
        input.Upsert(nameof(TemplatePlaceholderNameAndValues), tPNV);
    }

    public static void UpsertRoute(this IDictionary<object, object> input, Route route)
    {
        input.Upsert(nameof(Route), route);
    }

    public static void UpsertErrors(this IDictionary<object, object> input, List<Error> errors)
    {
        input.Upsert(nameof(Errors), errors);
    }

    public static void SetError(this IDictionary<object, object> input, Error error)
    {
        var errors = new List<Error> { error };
        input.Upsert(nameof(Errors), errors);
    }

    public static void SetIInternalConfiguration(this IDictionary<object, object> input, IInternalConfiguration config)
    {
        input.Upsert(nameof(IInternalConfiguration), config);
    }

    public static IInternalConfiguration IInternalConfiguration(this IDictionary<object, object> input)
    {
        return input.Get<IInternalConfiguration>(nameof(Configuration.IInternalConfiguration));
    }

    public static List<Error> Errors(this IDictionary<object, object> input)
    {
        var errors = input.Get<List<Error>>(nameof(Errors));
        return errors ?? [];
    }

    public static Route Route(this IDictionary<object, object> input)
        => input.Get<Route>(nameof(Configuration.Route));

    public static List<PlaceholderNameAndValue> TemplatePlaceholderNameAndValues(this IDictionary<object, object> input)
        => input.Get<List<PlaceholderNameAndValue>>(nameof(Configuration.Route.TemplatePlaceholderNameAndValues));

    public static DownstreamRequest DownstreamRequest(this IDictionary<object, object> input) =>
        input.Get<DownstreamRequest>(nameof(Request.Middleware.DownstreamRequest));

    public static DownstreamResponse DownstreamResponse(this IDictionary<object, object> input) =>
        input.Get<DownstreamResponse>(nameof(Middleware.DownstreamResponse));

    public static DownstreamRoute DownstreamRoute(this IDictionary<object, object> input) =>
        input.Get<DownstreamRoute>(nameof(Configuration.DownstreamRoute));

    private static T Get<T>(this IDictionary<object, object> input, string key)
        => input.TryGetValue(key, out var value) ? (T)value : default;

    private static void Upsert<T>(this IDictionary<object, object> input, string key, T value)
    {
        // TODO Re-implement with TryAdd
        if (input.ContainsKey(key))
            input.Remove(key);

        input.Add(key, value);
    }
}
