using Ocelot.Errors;
using HttpStatus = System.Net.HttpStatusCode;

namespace Ocelot.Provider.Consul;

public class UnableToSetConfigInConsulError : Error
{
    public UnableToSetConfigInConsulError(string s)
        : base(s, OcelotErrorCode.UnknownError, (int)HttpStatus.NotFound)
    {
    }
}
