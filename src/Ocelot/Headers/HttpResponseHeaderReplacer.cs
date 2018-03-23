using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Ocelot.Configuration;
using Ocelot.Infrastructure;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Request.Middleware;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public class HttpResponseHeaderReplacer : IHttpResponseHeaderReplacer
    {
        private IPlaceholders _placeholders;

        public HttpResponseHeaderReplacer(IPlaceholders placeholders)
        {
            _placeholders = placeholders;
        }

        public Response Replace(HttpResponseMessage response, List<HeaderFindAndReplace> fAndRs, DownstreamRequest request)
        {
            foreach (var f in fAndRs)
            {
                //if the response headers contain a matching find and replace
                if(response.Headers.TryGetValues(f.Key, out var values))
                {
                    //check to see if it is a placeholder in the find...
                    var placeholderValue = _placeholders.Get(f.Find, request);

                    if(!placeholderValue.IsError)
                    {
                        //if it is we need to get the value of the placeholder
                        //var find = replacePlaceholder(httpRequestMessage);
                        var replaced = values.ToList()[f.Index].Replace(placeholderValue.Data, f.Replace.LastCharAsForwardSlash());
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
