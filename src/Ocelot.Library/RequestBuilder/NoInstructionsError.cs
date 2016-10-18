using Ocelot.Library.Errors;

namespace Ocelot.Library.RequestBuilder
{
    public class NoInstructionsError : Error
    {
        public NoInstructionsError(string splitToken) 
            : base($"There we no instructions splitting on {splitToken}", OcelotErrorCode.NoInstructionsError)
        {
        }
    }
}
