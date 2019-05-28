namespace Ocelot.Provider.Consul
{
    using Errors;

    public class UnableToSetConfigInConsulError : Error
    {
        public UnableToSetConfigInConsulError(string s)
            : base(s, OcelotErrorCode.UnknownError)
        {
        }
    }
}
