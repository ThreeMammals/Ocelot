using System;
using Ocelot.Library.Errors;

namespace Ocelot.Library.RequestBuilder
{
    public class ParsingConfigurationHeaderError : Error
    {
        public ParsingConfigurationHeaderError(Exception exception) 
            : base($"error parsing configuration eception is {exception.Message}", OcelotErrorCode.ParsingConfigurationHeaderError)
        {
        }
    }
}
