using Ocelot.Errors;

namespace Ocelot.Provider.Consul
{
    public class UnableToSetConfigInConsulError : Error
    {
        public UnableToSetConfigInConsulError(string s)
            : base(s, OcelotErrorCode.UnknownError, 404)
        {
        }
    }
}
