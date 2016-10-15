using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Errors;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Responder
{
    public interface IErrorsToHttpStatusCodeMapper
    {
        Response<int> Map(List<Error> errors);
    }
}
