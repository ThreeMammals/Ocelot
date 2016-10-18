using Ocelot.Library.Responses;

namespace Ocelot.Library.Configuration.Parser
{
    public interface IClaimToHeaderConfigurationParser
    {
        Response<ClaimToHeader> Extract(string headerKey, string value);
    }
}