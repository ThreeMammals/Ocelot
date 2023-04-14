using Microsoft.AspNetCore.Http;

namespace Ocelot.Headers
{
    using System.Collections.Generic;

    using Configuration;
    using Configuration.Creator;
    using Ocelot.Request.Middleware;
    using Responses;

    public interface IAddHeadersToRequest
    {
        Response SetHeadersOnDownstreamRequest(List<ClaimToThing> claimsToThings, IEnumerable<System.Security.Claims.Claim> claims, DownstreamRequest downstreamRequest);

        void SetHeadersOnDownstreamRequest(IEnumerable<AddHeader> headers, HttpContext context);
    }
}
