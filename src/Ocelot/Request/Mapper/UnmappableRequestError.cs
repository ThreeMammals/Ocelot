namespace Ocelot.Request.Mapper
{
    using Errors;
    using System;

    public class UnmappableRequestError : Error
    {
        public UnmappableRequestError(Exception exception) : base($"Error when parsing incoming request, exception: {exception}", OcelotErrorCode.UnmappableRequestError)
        {
        }
    }
}
