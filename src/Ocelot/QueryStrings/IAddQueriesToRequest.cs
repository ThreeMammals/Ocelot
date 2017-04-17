using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;
using System.Net.Http;
using System.Security.Claims;

namespace Ocelot.QueryStrings
{
    public interface IAddQueriesToRequest
    {
        //Response SetQueriesOnContext(List<ClaimToThing> claimsToThings,
        //    HttpContext context);

        Response SetQueriesOnDownstreamRequest(List<ClaimToThing> claimsToThings, IEnumerable<Claim> claims, HttpRequestMessage downstreamRequest);
    }
}
