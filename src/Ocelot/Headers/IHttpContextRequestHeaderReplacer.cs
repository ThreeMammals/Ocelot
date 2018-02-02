using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IHttpContextRequestHeaderReplacer
    {
        Response Replace(HttpContext context, List<HeaderFindAndReplace> fAndRs);
    }
}