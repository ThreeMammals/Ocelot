using Ocelot.Errors;

namespace Ocelot.Configuration.Repository
{
    public class UnableToSetConfigInConsulError : Error
    {
        public UnableToSetConfigInConsulError(string message) 
            : base(message, OcelotErrorCode.UnableToSetConfigInConsulError)
        {
        }
    }
}