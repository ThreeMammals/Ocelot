using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.HeaderBuilder
{
    public interface IAddHeadersToRequest
    {
        Response SetHeadersOnContext(List<ClaimToHeader> configurationHeaderExtractorProperties,
            HttpContext context);
    }
}
