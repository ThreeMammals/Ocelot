using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Claims
{
    public interface IAddClaimsToRequest
    {
        Response SetClaimsOnContext(List<ClaimToThing> claimsToThings,
            HttpContext context);
    }
}
