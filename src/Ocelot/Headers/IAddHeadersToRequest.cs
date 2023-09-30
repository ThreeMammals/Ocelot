using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Request.Middleware;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IAddHeadersToRequest
    {
        Response SetHeadersOnDownstreamRequest(List<ClaimToThing> claimsToThings, IEnumerable<System.Security.Claims.Claim> claims, DownstreamRequest downstreamRequest);

        void SetHeadersOnDownstreamRequest(IEnumerable<AddHeader> headers, HttpContext context);
    }
}
