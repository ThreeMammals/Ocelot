using Microsoft.Extensions.Options;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Header = System.Collections.Generic.KeyValuePair<string, string>;

namespace Ocelot.Configuration.Creator;

public class HeaderFindAndReplaceCreator : IHeaderFindAndReplaceCreator
{
    private readonly IPlaceholders _placeholders;
    private readonly IOcelotLogger _logger;
    private readonly FileGlobalConfiguration _globalConfiguration;

    public HeaderFindAndReplaceCreator(IPlaceholders placeholders, IOcelotLoggerFactory factory, IOptions<FileGlobalConfiguration> global)
    {
        _placeholders = placeholders;
        _logger = factory.CreateLogger<HeaderFindAndReplaceCreator>();
        _globalConfiguration = global.Value;
    }

    public HeaderTransformations Create(FileRoute route)
        => Create(route, _globalConfiguration);

    public HeaderTransformations Create(FileRoute route, FileGlobalConfiguration global)
    {
        global ??= _globalConfiguration;
        var upstreamTransform = Merge(route.UpstreamHeaderTransform, global.UpstreamHeaderTransform);
        var (upstream, addHeadersToUpstream) = ProcessHeaders(upstreamTransform, nameof(route.UpstreamHeaderTransform));

        var downstreamTransform = Merge(route.DownstreamHeaderTransform, global.DownstreamHeaderTransform);
        var (downstream, addHeadersToDownstream) = ProcessHeaders(downstreamTransform, nameof(route.DownstreamHeaderTransform));
        
        return new HeaderTransformations(upstream, downstream, addHeadersToDownstream, addHeadersToUpstream);
    }

    /// <summary>Merge global Up/Downstream settings to the Route local ones.</summary>
    /// <param name="local">The Route local settings.</param>
    /// <param name="global">Global default settings.</param>
    /// <returns> An <see cref="IEnumerable{T}"/> collection where T is <see cref="Header"/>.</returns>
    public static IEnumerable<Header> Merge(IDictionary<string, string> local, IDictionary<string, string> global)
    {
        // Winning strategy: The Route local setting wins over global one
        var toAdd = global.ExceptBy(local.Keys, x => x.Key);
        return local.Union(toAdd);
    }

    private (List<HeaderFindAndReplace> StreamHeaders, List<AddHeader> AddHeaders)
        ProcessHeaders(IEnumerable<Header> headerTransform, string propertyName)
    {
        var addHeaders = new List<AddHeader>();
        var streamHeaders = new List<HeaderFindAndReplace>();

        var headerPairs = headerTransform ?? Enumerable.Empty<Header>();
        foreach (var input in headerPairs)
        {
            if (input.Value.Contains(HeaderFindAndReplace.Comma))
            {
                var hAndr = Map(input);
                if (hAndr != null)
                {
                    streamHeaders.Add(hAndr);
                }
                else
                {
                    _logger.LogWarning(() => $"Unable to add {propertyName} {input}");
                }
            }
            else
            {
                addHeaders.Add(new AddHeader(input.Key, input.Value));
            }
        }

        return (streamHeaders, addHeaders);
    }

    private HeaderFindAndReplace Map(Header input)
    {
        var findAndReplace = input.Value.Split(HeaderFindAndReplace.Comma);
        var replace = findAndReplace[1].TrimStart();

        var startOfPlaceholder = replace.IndexOf(Placeholders.OpeningBrace, StringComparison.Ordinal);
        if (startOfPlaceholder > -1)
        {
            var endOfPlaceholder = replace.IndexOf(Placeholders.ClosingBrace, startOfPlaceholder);
            var placeholder = replace.Substring(startOfPlaceholder,
                                                endOfPlaceholder - startOfPlaceholder + 1);
            var value = _placeholders.Get(placeholder);
            if (value.IsError)
            {
                _logger.LogWarning(() => $"{nameof(HeaderFindAndReplace)} was not mapped from {input} due to {value.Errors.ToErrorString()}");
                return null;
            }

            replace = replace.Replace(placeholder, value.Data);
        }

        return new(input.Key, findAndReplace[0], replace, 0);
    }
}
