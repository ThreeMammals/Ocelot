namespace Ocelot.ClaimsBuilder
{
    using System.Collections.Generic;
    using Configuration;
    using Microsoft.AspNetCore.Http;
    using Responses;

    public interface IAddClaimsToRequest
    {
        Response SetClaimsOnContext(List<ClaimToThing> claimsToThings,
            HttpContext context);
    }
}
