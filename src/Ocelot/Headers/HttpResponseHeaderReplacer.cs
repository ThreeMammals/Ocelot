using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Ocelot.Configuration;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public class HttpResponseHeaderReplacer : IHttpResponseHeaderReplacer
    {
        private Dictionary<string, Func<HttpRequestMessage, string>> _placeholders;

        public HttpResponseHeaderReplacer()
        {
            _placeholders = new Dictionary<string, Func<HttpRequestMessage, string>>();
            _placeholders.Add("{DownstreamBaseUrl}", x => {
                var downstreamUrl = $"{x.RequestUri.Scheme}://{x.RequestUri.Host}";

                if(x.RequestUri.Port != 80 && x.RequestUri.Port != 443)
                {
                    downstreamUrl = $"{downstreamUrl}:{x.RequestUri.Port}";
                }

                return $"{downstreamUrl}/";
            });
        }
        public Response Replace(HttpResponseMessage response, List<HeaderFindAndReplace> fAndRs, HttpRequestMessage httpRequestMessage)
        {
            foreach (var f in fAndRs)
            {
                //if the response headers contain a matching find and replace
                if(response.Headers.TryGetValues(f.Key, out var values))
                {
                    //check to see if it is a placeholder in the find...
                    if(_placeholders.TryGetValue(f.Find, out var replacePlaceholder))
                    {
                        //if it is we need to get the value of the placeholder
                        var find = replacePlaceholder(httpRequestMessage);
                        var replaced = values.ToList()[f.Index].Replace(find, f.Replace.LastCharAsForwardSlash());
                        response.Headers.Remove(f.Key);
                        response.Headers.Add(f.Key, replaced);
                    }
                    else
                    {
                        var replaced = values.ToList()[f.Index].Replace(f.Find, f.Replace);
                        response.Headers.Remove(f.Key);
                        response.Headers.Add(f.Key, replaced);
                    }
                }
            }

            return new OkResponse();
        }
    }
}