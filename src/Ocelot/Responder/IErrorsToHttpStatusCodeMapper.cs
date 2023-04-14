namespace Ocelot.Responder
{
    using System.Collections.Generic;

    using Errors;

    /// <summary>
    /// Map a list OceoltErrors to a single appropriate HTTP status code
    /// </summary>
    public interface IErrorsToHttpStatusCodeMapper
    {
        int Map(List<Error> errors);
    }
}
