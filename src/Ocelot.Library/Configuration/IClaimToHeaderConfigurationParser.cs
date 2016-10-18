using Ocelot.Library.Responses;

namespace Ocelot.Library.Configuration
{
    public interface IClaimToHeaderConfigurationParser
    {
        Response<ClaimToHeader> Extract(string headerKey, string value);
    }
}