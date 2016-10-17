namespace Ocelot.Library.Responder
{
    using System.Collections.Generic;
    using Errors;
    using Responses;

    public interface IErrorsToHttpStatusCodeMapper
    {
        Response<int> Map(List<Error> errors);
    }
}
