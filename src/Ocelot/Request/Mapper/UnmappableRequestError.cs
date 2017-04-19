namespace Ocelot.Request.Mapper
{
    using Ocelot.Errors;
    using System;

    public class UnmappableRequestError : Error
    {
        public UnmappableRequestError(Exception ex) : base($"Error when parsing incoming request, exception: {ex.Message}", OcelotErrorCode.UnmappableRequestError)
        {
        }
    }
}
