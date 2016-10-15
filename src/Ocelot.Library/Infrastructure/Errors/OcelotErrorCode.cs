namespace Ocelot.Library.Infrastructure.Errors
{
    public enum OcelotErrorCode
    {
        UnauthenticatedError, 
        UnknownError,
        DownstreamTemplateAlreadyUsedError,
        UnableToFindDownstreamRouteError,
        CannotAddDataError,
        CannotFindDataError,
        UnableToCompleteRequestError,
        UnableToCreateAuthenticationHandlerError
    }
}
