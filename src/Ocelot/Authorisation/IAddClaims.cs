using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Authorisation
{
    public interface IAddClaims
    {
        Response SetHeadersOnContext(List<ClaimToHeader> configurationHeaderExtractorProperties,
            HttpContext context);
    }
}
