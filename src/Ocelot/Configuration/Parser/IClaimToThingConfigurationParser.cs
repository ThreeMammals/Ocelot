using Ocelot.Responses;

namespace Ocelot.Configuration.Parser
{
    public interface IClaimToThingConfigurationParser
    {
        Response<ClaimToThing> Extract(string existingKey, string value);
    }
}