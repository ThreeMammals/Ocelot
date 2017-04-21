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
        Response SetQueriesOnDownstreamRequest(List<ClaimToThing> claimsToThings, IEnumerable<Claim> claims, HttpRequestMessage downstreamRequest);
    }
}
