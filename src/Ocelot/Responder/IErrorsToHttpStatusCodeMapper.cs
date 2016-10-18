using System.Collections.Generic;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.Responder
{
    public interface IErrorsToHttpStatusCodeMapper
    {
        Response<int> Map(List<Error> errors);
    }
}
