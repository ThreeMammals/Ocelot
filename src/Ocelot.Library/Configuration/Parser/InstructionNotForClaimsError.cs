using Ocelot.Library.Errors;

namespace Ocelot.Library.Configuration.Parser
{
    public class InstructionNotForClaimsError : Error
    {
        public InstructionNotForClaimsError() 
            : base("instructions did not contain claims, at the moment we only support claims extraction", OcelotErrorCode.InstructionNotForClaimsError)
        {
        }
    }
}
