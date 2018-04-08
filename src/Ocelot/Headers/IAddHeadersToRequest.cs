namespace Ocelot.Headers
{
    using System.Collections.Generic;
    using Configuration;
    using Request.Middleware;
    using Responses;

    public interface IAddHeadersToRequest
    {
        Response SetHeadersOnDownstreamRequest(List<ClaimToThing> claimsToThings, IEnumerable<System.Security.Claims.Claim> claims, DownstreamRequest downstreamRequest);
    }
}
