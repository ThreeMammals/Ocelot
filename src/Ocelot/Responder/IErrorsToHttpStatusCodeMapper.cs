using System.Collections.Generic;
using Ocelot.Errors;

namespace Ocelot.Responder
{
    /// <summary>
    /// Map a list OceoltErrors to a single appropriate HTTP status code
    /// </summary>
    public interface IErrorsToHttpStatusCodeMapper
    {
        int Map(List<Error> errors);
    }
}
