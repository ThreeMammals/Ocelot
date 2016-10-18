using System.Collections.Generic;
using System.Security.Claims;
using Ocelot.Library.Responses;

namespace Ocelot.Library.RequestBuilder
{
    public interface IClaimsParser
    {
        Response<string> GetValue(IEnumerable<Claim> claims, string key, string delimiter, int index);
    }
}