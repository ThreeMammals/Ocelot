using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public class HttpResponseHeaderReplacer : IHttpResponseHeaderReplacer
    {
        public Response Replace(HttpResponseMessage response, List<HeaderFindAndReplace> fAndRs)
        {
            foreach (var f in fAndRs)
            {
                if(response.Headers.TryGetValues(f.Key, out var values))
                {
                    var replaced = values.ToList()[f.Index].Replace(f.Find, f.Replace);
                    response.Headers.Remove(f.Key);
                    response.Headers.Add(f.Key, replaced);
                }
            }

            return new OkResponse();
        }
    }
}