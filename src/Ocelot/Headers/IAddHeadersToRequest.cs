namespace Ocelot.Headers
{
    using System.Collections.Generic;
    using System.Net.Http;

    using Ocelot.Configuration;
    using Ocelot.Responses;

    public interface IAddHeadersToRequest
    {
        Response SetHeadersOnDownstreamRequest(List<ClaimToThing> claimsToThings, IEnumerable<System.Security.Claims.Claim> claims, HttpRequestMessage downstreamRequest);
    }
}
