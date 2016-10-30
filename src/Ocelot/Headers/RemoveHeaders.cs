using System.Collections.Generic;
using System.Net.Http.Headers;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public class RemoveHeaders : IRemoveHeaders
    {
        /// <summary>
        /// Some webservers return headers that cannot be forwarded to the client
        /// in a given context such as transfer encoding chunked when ASP.NET is not
        /// returning the response in this manner
        /// </summary>
        private readonly List<string> _unsupportedHeaders = new List<string>
        {
            "Transfer-Encoding"
        };


        public Response Remove(HttpResponseHeaders headers)
        {
            foreach (var unsupported in _unsupportedHeaders)
            {
                headers.Remove(unsupported);
            }

            return new OkResponse();
        }
    }
}