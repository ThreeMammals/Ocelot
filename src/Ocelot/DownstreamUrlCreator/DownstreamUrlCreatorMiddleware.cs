using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System.Web;

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
        newQueryString = newQueryString.Replace(QuestionMark, Ampersand);
        var queries = HttpUtility.ParseQueryString(queryString);
        var newQueries = HttpUtility.ParseQueryString(newQueryString);

        // Remove old replaced query parameters
        var placeholderKeys = new HashSet<string>(placeholders.Select(p => p.Key));
        foreach (var queryKey in queries.AllKeys.Where(placeholderKeys.Contains))
        {
            queries.Remove(queryKey);
        }

        var parameters = newQueries.AllKeys
            .Where(key => key.IsNotEmpty())
            .ToDictionary(key => key, key => newQueries[key]);

        _ = queries.AllKeys
            .Where(key => key.IsNotEmpty() && !parameters.ContainsKey(key))
            .All(key => parameters.TryAdd(key, queries[key]));

        return QueryHelpers.AddQueryString(string.Empty, parameters);
    }

    //protected static string MapQueryParameter(KeyValuePair<string, string> pair) => $"{pair.Key}={pair.Value}";

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
