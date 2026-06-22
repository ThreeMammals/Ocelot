using Ocelot.Errors;

namespace Ocelot.Responses;

public abstract class Response
{
    protected Response()
    {
        Errors = new List<Error>();
    }

    protected Response(List<Error> errors)
    {
        Errors = errors ?? new List<Error>();
    }

    public List<Error> Errors { get; }

    public bool IsError => Errors.Count > 0;
}

public abstract class Response<T> : Response
{
    protected Response(T data)
    {
        Data = data;
    }

    protected Response(List<Error> errors) : base(errors)
    { }

    public T Data { get; }
}
