using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.QualityOfService;

public class UnableToFindQoSProviderError : Error
{
    public UnableToFindQoSProviderError(string message)
        : base(message, OcelotErrorCode.UnableToFindQoSProviderError, StatusCodes.Status404NotFound)
    { }
}
