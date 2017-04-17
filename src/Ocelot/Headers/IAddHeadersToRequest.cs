using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;
using System.Net.Http;

namespace Ocelot.Headers
{
    public interface IAddHeadersToRequest
    {
        //Response SetHeadersOnContext(List<ClaimToThing> claimsToThings,
        //    HttpContext context);

        Response SetHeadersOnDownstreamRequest(List<ClaimToThing> claimsToThings, IEnumerable<System.Security.Claims.Claim> claims, HttpRequestMessage downstreamRequest);
    }
}
