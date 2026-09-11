using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.Configuration.Parser;

public class ParsingConfigurationHeaderError : Error
{
    public ParsingConfigurationHeaderError(Exception exception)
        : base($"Parsing configuration exception is {exception.Message}",
            OcelotErrorCode.ParsingConfigurationHeaderError,
            StatusCodes.Status404NotFound)
    {
    }
}
