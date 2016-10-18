using Ocelot.Library.Errors;

namespace Ocelot.Library.Configuration.Parser
{
    public class NoInstructionsError : Error
    {
        public NoInstructionsError(string splitToken) 
            : base($"There we no instructions splitting on {splitToken}", OcelotErrorCode.NoInstructionsError)
        {
        }
    }
}
