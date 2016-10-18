using System.Collections.Generic;
using System.Security.Claims;
using Ocelot.Responses;

namespace Ocelot.Claims.Parser
{
    public interface IClaimsParser
    {
        Response<string> GetValue(IEnumerable<Claim> claims, string key, string delimiter, int index);
    }
}