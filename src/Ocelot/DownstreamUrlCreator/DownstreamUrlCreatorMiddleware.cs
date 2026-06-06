using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.DownstreamUrlCreator;

public class DownstreamUrlCreatorMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDownstreamPathPlaceholderReplacer _replacer;

    private const char Ampersand = '&';
    private const char QuestionMark = '?';
    protected const char Slash = '/';

    public DownstreamUrlCreatorMiddleware(
        RequestDelegate next,
        IOcelotLoggerFactory loggerFactory,
        IDownstreamPathPlaceholderReplacer replacer)
        : base(loggerFactory.CreateLogger<DownstreamUrlCreatorMiddleware>())
    {
        _next = next;
        _replacer = replacer;
    }

    public async Task Invoke(HttpContext context)
    {
        var downstreamRoute = context.Items.DownstreamRoute();
        var placeholders = context.Items.TemplatePlaceholderNameAndValues();
        var downstreamPath = _replacer.Replace(downstreamRoute.DownstreamPathTemplate.Value, placeholders);
        if (downstreamPath.Value.IsEmpty())
        {
            throw new NotSupportedException($"{_replacer.GetType().Name} returned an empty {nameof(DownstreamPath)} for the route {downstreamRoute.Name()}.");
        }

        var dsPath = downstreamPath.Value;
        var downstreamRequest = context.Items.DownstreamRequest();
        var upstreamPath = downstreamRequest.AbsolutePath;
        if (dsPath.EndsWith(Slash) && !upstreamPath.EndsWith(Slash))
        {
            dsPath = dsPath.TrimEnd(Slash);
            downstreamPath = new DownstreamPath(dsPath);
        }

        if (!string.IsNullOrEmpty(downstreamRoute.DownstreamScheme))
        {
            // TODO Make sure this works, hopefully there is a test ;E
            context.Items.DownstreamRequest().Scheme = downstreamRoute.DownstreamScheme;
        }

        var internalConfiguration = context.Items.IInternalConfiguration();

        if (ServiceFabricRequest(internalConfiguration, downstreamRoute))
        {
            var (path, query) = CreateServiceFabricUri(downstreamRequest, downstreamRoute, placeholders, downstreamPath);

            // TODO Check this works again hope there is a test..
            downstreamRequest.AbsolutePath = path;
            downstreamRequest.Query = query;
        }
        else
        {
            if (dsPath.Contains(QuestionMark))
            {
                downstreamRequest.AbsolutePath = GetPath(dsPath).ToString();
                var newQuery = GetQueryString(dsPath).ToString();
                downstreamRequest.Query = string.IsNullOrEmpty(downstreamRequest.Query)
                    ? newQuery
                    : MergeQueryStringsWithoutDuplicateValues(downstreamRequest.Query, newQuery, placeholders);
            }
            else
            {
                downstreamRequest.AbsolutePath = dsPath;
                downstreamRequest.Query = RemoveQueryStringParametersThatHaveBeenUsedInTemplate(downstreamRequest, placeholders);
            }
        }

        Logger.LogDebug(() => $"Downstream URL: {downstreamRequest}");

        await _next.Invoke(context);
    }

    /// <summary>
    /// <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/routing.rst#merging-of-query-parameters">Merging of Query Parameters</see> is part of
    /// the <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/routing.rst#query-placeholders">Query Placeholders</see> feature.
    /// </summary>
    /// <returns>A <see cref="string"/> object.</returns>
    protected static string MergeQueryStringsWithoutDuplicateValues(string queryString, string newQueryString, List<PlaceholderNameAndValue> placeholders)
    {
        var queries = ParseQueryStringPreservingPlus(queryString);
        var newQueries = ParseQueryStringPreservingPlus(newQueryString);

        // Remove old replaced query parameters
        var placeholderKeys = new HashSet<string>(placeholders.Select(p => p.Key));
        foreach (var queryKey in queries.Keys.Where(placeholderKeys.Contains).ToArray())
        {
            queries.Remove(queryKey);
        }

        var parameters = newQueries
            .Where(pair => pair.Key.IsNotEmpty())
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (var pair in queries.Where(pair => pair.Key.IsNotEmpty() && !parameters.ContainsKey(pair.Key)))
        {
            parameters.TryAdd(pair.Key, pair.Value);
        }

        return QueryHelpers.AddQueryString(string.Empty, parameters);
    }

    private static Dictionary<string, StringValues> ParseQueryStringPreservingPlus(string queryString)
    {
        if (string.IsNullOrEmpty(queryString))
        {
            return new Dictionary<string, StringValues>();
        }

        var query = queryString.StartsWith(QuestionMark)
            ? queryString[1..]
            : queryString;

        var result = new Dictionary<string, StringValues>();
        foreach (var segment in query.Split(Ampersand, StringSplitOptions.RemoveEmptyEntries))
        {
            var equalsIndex = segment.IndexOf('=');
            var key = equalsIndex < 0
                ? segment
                : segment[..equalsIndex];
            var value = equalsIndex < 0
                ? string.Empty
                : segment[(equalsIndex + 1)..];

            key = UrlDecodePreservingPlus(key);
            value = UrlDecodePreservingPlus(value);

            if (result.TryGetValue(key, out var existing))
            {
                result[key] = StringValues.Concat(existing, value);
            }
            else
            {
                result.Add(key, value);
            }
        }

        return result;
    }

    private static string UrlDecodePreservingPlus(string value)
    {
        if (!value.Contains('%'))
        {
            return value;
        }

        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch (UriFormatException)
        {
            return value;
        }
    }

    /// <summary>
    /// Feature 467: <see href="https://github.com/ThreeMammals/Ocelot/pull/467">Added support for query string parameters in upstream path template</see>.
    /// </summary>
    /// <returns>A <see cref="string"/> object without wanted parameters.</returns>
    protected static string RemoveQueryStringParametersThatHaveBeenUsedInTemplate(DownstreamRequest request, List<PlaceholderNameAndValue> templatePlaceholders)
    {
        if (templatePlaceholders.Count == 0 || request.Query.IsEmpty())
        {
            return request.Query;
        }

        var query = QueryHelpers.ParseQuery(request.Query);
        foreach (var placeholder in templatePlaceholders.Where(p => query.ContainsKey(p.Key)))
        {
            query.Remove(placeholder.Key);
        }

        return QueryHelpers.AddQueryString(string.Empty, query);
    }

    protected static ReadOnlySpan<char> GetPath(ReadOnlySpan<char> downstreamPath)
    {
        int length = downstreamPath.IndexOf(QuestionMark);
        return length >= 0
            ? downstreamPath[..length]
            : downstreamPath;
    }

    protected static ReadOnlySpan<char> GetQueryString(ReadOnlySpan<char> downstreamPath)
    {
        int startIndex = downstreamPath.IndexOf(QuestionMark);
        return startIndex >= 0
            ? downstreamPath[startIndex..]
            : ReadOnlySpan<char>.Empty;
    }

    protected (string Path, string Query) CreateServiceFabricUri(DownstreamRequest downstreamRequest, DownstreamRoute downstreamRoute, List<PlaceholderNameAndValue> templatePlaceholderNameAndValues, DownstreamPath dsPath)
    {
        var query = downstreamRequest.Query;
        var serviceName = _replacer.Replace(downstreamRoute.ServiceName, templatePlaceholderNameAndValues);
        var pathTemplate = $"/{serviceName.Value}{dsPath.Value}";
        return (pathTemplate, query);
    }

    protected static bool ServiceFabricRequest(IInternalConfiguration config, DownstreamRoute route)
        => ServiceFabricServiceDiscoveryProvider.Type.Equals(config.ServiceProviderConfiguration.Type, StringComparison.OrdinalIgnoreCase)
            && route.UseServiceDiscovery;
}
