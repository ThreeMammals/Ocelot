namespace Ocelot.Errors
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
        InstructionNotForClaimsError,
        UnauthorizedError
    }
}
