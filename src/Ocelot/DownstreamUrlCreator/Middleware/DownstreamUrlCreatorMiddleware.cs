using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Values;
using System.Web;

namespace Ocelot.DownstreamUrlCreator.Middleware;

public class DownstreamUrlCreatorMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDownstreamPathPlaceholderReplacer _replacer;

    private const char Ampersand = '&';
    private const char QuestionMark = '?';
    private const char OpeningBrace = '{';
    private const char ClosingBrace = '}';
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

    public async Task Invoke(HttpContext httpContext)
    {
        var downstreamRoute = httpContext.Items.DownstreamRoute();
        var placeholders = httpContext.Items.TemplatePlaceholderNameAndValues();
        var response = _replacer.Replace(downstreamRoute.DownstreamPathTemplate.Value, placeholders);
        var downstreamRequest = httpContext.Items.DownstreamRequest();
        var upstreamPath = downstreamRequest.AbsolutePath;

        if (response.IsError)
        {
            Logger.LogDebug($"{nameof(IDownstreamPathPlaceholderReplacer)} returned an error, setting pipeline error");

            httpContext.Items.UpsertErrors(response.Errors);
            return;
        }

        var dsPath = response.Data.Value;
        if (dsPath.EndsWith(Slash) && !upstreamPath.EndsWith(Slash))
        {
            dsPath = dsPath.TrimEnd(Slash);
            response = new OkResponse<DownstreamPath>(new DownstreamPath(dsPath));
        }

        if (!string.IsNullOrEmpty(downstreamRoute.DownstreamScheme))
        {
            // TODO Make sure this works, hopefully there is a test ;E
            httpContext.Items.DownstreamRequest().Scheme = downstreamRoute.DownstreamScheme;
        }

        var internalConfiguration = httpContext.Items.IInternalConfiguration();

        if (ServiceFabricRequest(internalConfiguration, downstreamRoute))
        {
            var (path, query) = CreateServiceFabricUri(downstreamRequest, downstreamRoute, placeholders, response);

            // TODO Check this works again hope there is a test..
            downstreamRequest.AbsolutePath = path;
            downstreamRequest.Query = query;
        }
        else
        {
            if (dsPath.Contains(QuestionMark))
            {
                downstreamRequest.AbsolutePath = GetPath(dsPath);
                var newQuery = GetQueryString(dsPath);
                downstreamRequest.Query = string.IsNullOrEmpty(downstreamRequest.Query)
                    ? newQuery
                    : MergeQueryStringsWithoutDuplicateValues(downstreamRequest.Query, newQuery, placeholders);
            }
            else
            {
                RemoveQueryStringParametersThatHaveBeenUsedInTemplate(downstreamRequest, placeholders);
                downstreamRequest.AbsolutePath = dsPath;
            }
        }

        Logger.LogDebug(() => $"Downstream url is {downstreamRequest}");

        await _next.Invoke(httpContext);
    }

    private static string MergeQueryStringsWithoutDuplicateValues(string queryString, string newQueryString, List<PlaceholderNameAndValue> placeholders)
    {
        newQueryString = newQueryString.Replace(QuestionMark, Ampersand);
        var queries = HttpUtility.ParseQueryString(queryString);
        var newQueries = HttpUtility.ParseQueryString(newQueryString);

        // Remove old replaced query parameters
        var placeholderNames = new HashSet<string>(placeholders.Select(p => p.Name.Trim(OpeningBrace, ClosingBrace)));
        foreach (var queryKey in queries.AllKeys.Where(placeholderNames.Contains))
        {
            queries.Remove(queryKey);
        }

        var parameters = newQueries.AllKeys
            .Where(key => !string.IsNullOrEmpty(key))
            .ToDictionary(key => key, key => newQueries[key]);

        _ = queries.AllKeys
            .Where(key => !string.IsNullOrEmpty(key) && !parameters.ContainsKey(key))
            .All(key => parameters.TryAdd(key, queries[key]));

        return QuestionMark + string.Join(Ampersand, parameters.Select(MapQueryParameter));
    }

    private static string MapQueryParameter(KeyValuePair<string, string> pair) => $"{pair.Key}={pair.Value}";

    /// <summary>
    /// Feature <see href="https://github.com/ThreeMammals/Ocelot/pull/467">467</see>:
    /// Added support for query string parameters in upstream path template.
    /// </summary>
    private static void RemoveQueryStringParametersThatHaveBeenUsedInTemplate(DownstreamRequest downstreamRequest, List<PlaceholderNameAndValue> templatePlaceholders)
    {
        var builder = new StringBuilder();
        foreach (var nAndV in templatePlaceholders)
        {
            var name = nAndV.Name.Trim(OpeningBrace, ClosingBrace);
            var parameter = $"{name}={nAndV.Value}";
            if (!downstreamRequest.Query.Contains(parameter))
            {
                continue;
            }

            int questionMarkOrAmpersand = downstreamRequest.Query.IndexOf(name, StringComparison.Ordinal);
            builder.Clear()
                .Append(downstreamRequest.Query)
                .Replace(parameter, string.Empty)
                .Remove(--questionMarkOrAmpersand, 1);
            downstreamRequest.Query = builder.Length > 0
                ? builder.Remove(0, 1).Insert(0, QuestionMark).ToString()
                : string.Empty;
        }
    }

    private static string GetPath(string downstreamPath)
    {
        int length = downstreamPath.IndexOf(QuestionMark, StringComparison.Ordinal);
        return downstreamPath[..length];
    }

    private static string GetQueryString(string downstreamPath)
    {
        int startIndex = downstreamPath.IndexOf(QuestionMark, StringComparison.Ordinal);
        return downstreamPath[startIndex..];
    }

    private (string Path, string Query) CreateServiceFabricUri(DownstreamRequest downstreamRequest, DownstreamRoute downstreamRoute, List<PlaceholderNameAndValue> templatePlaceholderNameAndValues, Response<DownstreamPath> dsPath)
    {
        var query = downstreamRequest.Query;
        var serviceName = _replacer.Replace(downstreamRoute.ServiceName, templatePlaceholderNameAndValues);
        var pathTemplate = $"/{serviceName.Data.Value}{dsPath.Data.Value}";
        return (pathTemplate, query);
    }

    private static bool ServiceFabricRequest(IInternalConfiguration config, DownstreamRoute downstreamRoute)
    {
        return config.ServiceProviderConfiguration.Type?.ToLower() == "servicefabric" && downstreamRoute.UseServiceDiscovery;
    }
}
