using System.Collections.Generic;
using Ocelot.Library.Responses;

namespace Ocelot.Library.RequestBuilder
{
    public interface IConfigurationHeaderExtrator
    {
        Response<ConfigurationHeaderExtractorProperties> Extract(string headerKey, string value);
    }
}