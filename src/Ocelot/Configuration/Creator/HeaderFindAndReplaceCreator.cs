using Microsoft.Extensions.Options;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using Header = System.Collections.Generic.KeyValuePair<string, string>;

namespace Ocelot.Configuration.Creator
{
    public class HeaderFindAndReplaceCreator : IHeaderFindAndReplaceCreator
    {
        private readonly FileGlobalConfiguration _fileGlobalConfiguration;
        private readonly IPlaceholders _placeholders;
        private readonly IOcelotLogger _logger;

        public HeaderFindAndReplaceCreator(IOptions<FileConfiguration> fileConfiguration, IPlaceholders placeholders, IOcelotLoggerFactory factory)
        {
            _logger = factory.CreateLogger<HeaderFindAndReplaceCreator>();
            _fileGlobalConfiguration = fileConfiguration.Value.GlobalConfiguration;
            _placeholders = placeholders;
        }

        public HeaderTransformations Create(FileRoute fileRoute)
        {
            var upstreamHeaderTransform = Merge(fileRoute.UpstreamHeaderTransform, _fileGlobalConfiguration.UpstreamHeaderTransform);
            var (upstream, addHeadersToUpstream) = ProcessHeaders(upstreamHeaderTransform, nameof(fileRoute.UpstreamHeaderTransform));

            var downstreamHeaderTransform = Merge(fileRoute.DownstreamHeaderTransform, _fileGlobalConfiguration.DownstreamHeaderTransform);
            var (downstream, addHeadersToDownstream) = ProcessHeaders(downstreamHeaderTransform, nameof(fileRoute.DownstreamHeaderTransform));
            
            return new HeaderTransformations(upstream, downstream, addHeadersToDownstream, addHeadersToUpstream);
        }

        private (List<HeaderFindAndReplace> StreamHeaders, List<AddHeader> AddHeaders) ProcessHeaders(IEnumerable<Header> headerTransform, string propertyName = null)
        {
            var headerPairs = headerTransform ?? Enumerable.Empty<Header>();

            var streamHeaders = new List<HeaderFindAndReplace>();
            var addHeaders = new List<AddHeader>();

            foreach (var input in headerPairs)
            {
                if (input.Value.Contains(','))
                {
                    var hAndr = Map(input);
                    if (!hAndr.IsError)
                    {
                        streamHeaders.Add(hAndr.Data);
                    }
                    else
                    {
                        var name = propertyName ?? "Headers Transformation";
                        _logger.LogWarning($"Unable to add {name} {input.Key}: {input.Value}");
                    }
                }
                else
                {
                    addHeaders.Add(new AddHeader(input.Key, input.Value));
                }
            }

            return (streamHeaders, addHeaders);
        }

        private Response<HeaderFindAndReplace> Map(Header input)
        {
            var findAndReplace = input.Value.Split(',');

            var replace = findAndReplace[1].TrimStart();

            var startOfPlaceholder = replace.IndexOf('{', StringComparison.Ordinal);
            if (startOfPlaceholder > -1)
            {
                var endOfPlaceholder = replace.IndexOf('}', startOfPlaceholder);

                var placeholder = replace.Substring(startOfPlaceholder, endOfPlaceholder - startOfPlaceholder + 1);

                var value = _placeholders.Get(placeholder);

                if (value.IsError)
                {
                    return new ErrorResponse<HeaderFindAndReplace>(value.Errors);
                }

                replace = replace.Replace(placeholder, value.Data);
            }

            var hAndr = new HeaderFindAndReplace(input.Key, findAndReplace[0], replace, 0);

            return new OkResponse<HeaderFindAndReplace>(hAndr);
        }

        /// <summary>
        /// Merge global Up/Downstream settings to the Route local ones.
        /// </summary>
        /// <param name="local">The Route local settings.</param>
        /// <param name="global">Global default settings.</param>
        /// <returns> An <see cref="IEnumerable{T}"/> collection.</returns>
        public static IEnumerable<Header> Merge(Dictionary<string, string> local, Dictionary<string, string> global)
        {
            // Winning strategy: The Route local setting wins over global one
            var toAdd = global.ExceptBy(local.Keys, x => x.Key);
            return local.Union(toAdd).ToList();
        }
    }
}
