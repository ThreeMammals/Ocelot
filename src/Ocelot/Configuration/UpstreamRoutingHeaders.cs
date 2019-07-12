using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Ocelot.Configuration
{
    public class UpstreamRoutingHeaders
    {
        public UpstreamRoutingHeaders(Dictionary<string, HashSet<string>> headers)
        {
            Headers = headers;
        }

        public bool Empty() => Headers.Count == 0;

        public bool HasAnyOf(IHeaderDictionary requestHeaders)
        {
            foreach (KeyValuePair<string, HashSet<string>> h in Headers)
            {
                if (requestHeaders.TryGetValue(h.Key, out var values))
                {
                    HashSet<string> requestHeaderValues = new HashSet<string>(values);
                    if (h.Value.Overlaps(requestHeaderValues))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool HasAllOf(IHeaderDictionary requestHeaders)
        {
            foreach (KeyValuePair<string, HashSet<string>> h in Headers)
            {
                if (!requestHeaders.TryGetValue(h.Key, out var values))
                {
                    return false;
                }

                HashSet<string> requestHeaderValues = new HashSet<string>(values);
                if (!h.Value.Overlaps(requestHeaderValues))
                {
                    return false;
                }
            }

            return true;
        }

        private Dictionary<string, HashSet<string>> Headers;
    }
}
