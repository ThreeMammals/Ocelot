using System;
using System.Collections.Generic;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.Configuration.Creator
{
    public class HeaderFindAndReplaceCreator : IHeaderFindAndReplaceCreator
    {
        private IPlaceholders _placeholders;
        private IOcelotLogger _logger;

        public HeaderFindAndReplaceCreator(IPlaceholders placeholders, IOcelotLoggerFactory factory)
        {
            _logger = factory.CreateLogger<HeaderFindAndReplaceCreator>();;
            _placeholders = placeholders;
        }

        public HeaderTransformations Create(FileReRoute fileReRoute)
        {
            var upstream = new List<HeaderFindAndReplace>();

            foreach(var input in fileReRoute.UpstreamHeaderTransform)
            {
                var hAndr = Map(input);
                if(!hAndr.IsError)
                {
                    upstream.Add(hAndr.Data);
                }
                else
                {
                    _logger.LogError($"Unable to add UpstreamHeaderTransform {input.Key}: {input.Value}");
                }
            }

            var downstream = new List<HeaderFindAndReplace>();
            var addHeadersToDownstream = new List<AddHeader>();

            foreach(var input in fileReRoute.DownstreamHeaderTransform)
            {
                if(input.Value.Contains(","))
                {
                    var hAndr = Map(input);
                    if(!hAndr.IsError)
                    {
                        downstream.Add(hAndr.Data);
                    }
                    else
                    {
                        _logger.LogError($"Unable to add DownstreamHeaderTransform {input.Key}: {input.Value}");
                    }
                }
                else
                {
                    addHeadersToDownstream.Add(new AddHeader(input.Key, input.Value));
                }
            }
            
            return new HeaderTransformations(upstream, downstream, addHeadersToDownstream);
        }

        private Response<HeaderFindAndReplace> Map(KeyValuePair<string,string> input)
        {
            var findAndReplace = input.Value.Split(",");

            var replace = findAndReplace[1].TrimStart();

            var startOfPlaceholder = replace.IndexOf("{");
            if(startOfPlaceholder > -1)
            {
                var endOfPlaceholder = replace.IndexOf("}", startOfPlaceholder);
                
                var placeholder = replace.Substring(startOfPlaceholder, startOfPlaceholder + (endOfPlaceholder + 1));

                var value = _placeholders.Get(placeholder);

                if(value.IsError)
                {
                    return new ErrorResponse<HeaderFindAndReplace>(value.Errors);
                }

                replace = replace.Replace(placeholder, value.Data);
            }

            var hAndr = new HeaderFindAndReplace(input.Key, findAndReplace[0], replace, 0);
            
            return new OkResponse<HeaderFindAndReplace>(hAndr);
        }
    }
}
