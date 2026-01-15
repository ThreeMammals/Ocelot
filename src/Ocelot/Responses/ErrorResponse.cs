using Ocelot.Errors;

namespace Ocelot.Responses;

public class ErrorResponse : Response
{
    public ErrorResponse(Error error)
        : base(new() { error })
    { }

    public ErrorResponse(List<Error> errors)
        : base(errors)
    { }
}

public class ErrorResponse<T> : Response<T>
{
    public ErrorResponse(Error error)
        : base(new List<Error> { error })
    { }

    public ErrorResponse(List<Error> errors)
        : base(errors)
    { }
}
