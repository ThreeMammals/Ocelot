namespace Ocelot.Library.Errors
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
        UnableToCreateAuthenticationHandlerError,
        UnsupportedAuthenticationProviderError,
        CannotFindClaimError,
        ParsingConfigurationHeaderError,
        NoInstructionsError,
        InstructionNotForClaimsError
    }
}
