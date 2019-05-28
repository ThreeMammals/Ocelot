namespace Ocelot.Headers
{
    using Ocelot.Configuration;
    using Ocelot.Middleware;
    using Ocelot.Responses;
    using System.Collections.Generic;

    public interface IHttpResponseHeaderReplacer
    {
        Response Replace(DownstreamContext context, List<HeaderFindAndReplace> fAndRs);
    }
}
