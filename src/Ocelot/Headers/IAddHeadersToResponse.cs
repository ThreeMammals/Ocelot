namespace Ocelot.Headers
{
    using System.Collections.Generic;
    using System.Net.Http;
    using Ocelot.Configuration.Creator;

    public interface IAddHeadersToResponse
    {
        void Add(List<AddHeader> addHeaders, HttpResponseMessage response);
    }
}
