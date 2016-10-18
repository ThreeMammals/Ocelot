using Ocelot.Responses;

namespace Ocelot.Configuration.Parser
{
    public interface IClaimToHeaderConfigurationParser
    {
        Response<ClaimToHeader> Extract(string headerKey, string value);
    }
}