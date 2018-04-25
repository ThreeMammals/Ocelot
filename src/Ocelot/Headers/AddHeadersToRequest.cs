using System.Collections.Generic;
using System.Linq;
using Ocelot.Configuration;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Responses;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Creator;
using Ocelot.Request.Middleware;

namespace Ocelot.Headers
{
    public class AddHeadersToRequest : IAddHeadersToRequest
    {
        private readonly IClaimsParser _claimsParser;

        public AddHeadersToRequest(IClaimsParser claimsParser)
        {
            _claimsParser = claimsParser;
        }

        public Response SetHeadersOnDownstreamRequest(List<ClaimToThing> claimsToThings, IEnumerable<System.Security.Claims.Claim> claims, DownstreamRequest downstreamRequest)
        {
            foreach (var config in claimsToThings)
            {
                var value = _claimsParser.GetValue(claims, config.NewKey, config.Delimiter, config.Index);

                if (value.IsError)
                {
                    return new ErrorResponse(value.Errors);
                }

                var exists = downstreamRequest.Headers.FirstOrDefault(x => x.Key == config.ExistingKey);

                if (!string.IsNullOrEmpty(exists.Key))
                {
                    downstreamRequest.Headers.Remove(exists.Key);
                }

                downstreamRequest.Headers.Add(config.ExistingKey, value.Data);
            }

            return new OkResponse();
        }
        
        public void SetHeadersOnDownstreamRequest(IEnumerable<AddHeader> headers, HttpContext context)
        {
            var requestHeader = context.Request.Headers;
            foreach (var header in headers)
            {
                if (requestHeader.ContainsKey(header.Key))
                {
                    requestHeader.Remove(header.Key);
                }

                requestHeader.Add(header.Key, header.Value);
            }
        }
    }
}
