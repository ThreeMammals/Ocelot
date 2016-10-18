using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Responses;

namespace Ocelot.Library.RequestBuilder
{
    public interface IAddHeadersToRequest
    {
        Response SetHeadersOnContext(List<ConfigurationHeaderExtractorProperties> configurationHeaderExtractorProperties,
            HttpContext context);
    }
}
