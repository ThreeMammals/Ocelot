using System;
using Ocelot.Errors;

namespace Ocelot.Configuration.Parser
{
    public class ParsingConfigurationHeaderError : Error
    {
        public ParsingConfigurationHeaderError(Exception exception) 
            : base($"error parsing configuration eception is {exception.Message}", OcelotErrorCode.ParsingConfigurationHeaderError)
        {
        }
    }
}
