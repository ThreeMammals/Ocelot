using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Responses;

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
            var upstream = new List<HeaderFindAndReplace>();
            var addHeadersToUpstream = new List<AddHeader>();
            var upstreamAdded = new HashSet<string>();

            var upstreamHeaderTransform = Merge(fileRoute.UpstreamHeaderTransform, _fileGlobalConfiguration.UpstreamHeaderTransform);

            foreach (var input in upstreamHeaderTransform)
            {
                if (input.Value.Contains(','))
                {
                    var hAndr = Map(input);
                    if (!hAndr.IsError)
                    {
                        upstream.Add(hAndr.Data);
                        upstreamAdded.Add(input.Key);
                    }
                    else
                    {
                        _logger.LogWarning($"Unable to add UpstreamHeaderTransform {input.Key}: {input.Value}");
                    }
                }
                else
                {
                    addHeadersToUpstream.Add(new AddHeader(input.Key, input.Value));
                    upstreamAdded.Add(input.Key);
                }
            }
            
            var downstream = new List<HeaderFindAndReplace>();
            var addHeadersToDownstream = new List<AddHeader>();
            var downstreamAdded = new HashSet<string>();

            var downstreamHeaderTransform = Merge(fileRoute.DownstreamHeaderTransform, _fileGlobalConfiguration.DownstreamHeaderTransform);

            foreach (var input in downstreamHeaderTransform)
            {
                if (input.Value.Contains(','))
                {
                    var hAndr = Map(input);
                    if (!hAndr.IsError)
                    {
                        downstream.Add(hAndr.Data);
                        downstreamAdded.Add(input.Key);
                    }
                    else
                    {
                        _logger.LogWarning($"Unable to add DownstreamHeaderTransform {input.Key}: {input.Value}");
                    }
                }
                else
                {
                    addHeadersToDownstream.Add(new AddHeader(input.Key, input.Value));
                    downstreamAdded.Add(input.Key);
                }
            }
            
            return new HeaderTransformations(upstream, downstream, addHeadersToDownstream, addHeadersToUpstream);
        }

        private Response<HeaderFindAndReplace> Map(KeyValuePair<string, string> input)
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
        public static IEnumerable<KeyValuePair<string, string>> Merge(Dictionary<string, string> local, Dictionary<string, string> global)
        {
            // Winning strategy: The Route local setting wins over global one
            var toAdd = global.ExceptBy(local.Keys, x => x.Key);
            return local.Union(toAdd).ToList();
        }
    }
}
