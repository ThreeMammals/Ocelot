using System.Collections.Generic;
using System.Security.Claims;

using Ocelot.Configuration;
using Ocelot.Request.Middleware;
using Ocelot.Responses;

namespace Ocelot.QueryStrings
{
    public interface IAddQueriesToRequest
    {
        Response SetQueriesOnDownstreamRequest(List<ClaimToThing> claimsToThings, IEnumerable<Claim> claims, DownstreamRequest downstreamRequest);
    }
}
