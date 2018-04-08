﻿using System.Collections.Generic;
using Ocelot.Configuration;
using Ocelot.Responses;
using System.Security.Claims;
using Ocelot.Request.Middleware;

namespace Ocelot.QueryStrings
{
    public interface IAddQueriesToRequest
    {
        Response SetQueriesOnDownstreamRequest(List<ClaimToThing> claimsToThings, IEnumerable<Claim> claims, DownstreamRequest downstreamRequest);
    }
}
