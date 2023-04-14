namespace Ocelot.Request.Mapper
{
    using System;

    using Errors;

    public class UnmappableRequestError : Error
    {
        public UnmappableRequestError(Exception exception) : base($"Error when parsing incoming request, exception: {exception}", OcelotErrorCode.UnmappableRequestError, 404)
        {
        }
    }
}
