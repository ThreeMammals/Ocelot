namespace Ocelot.Responses;

public class OkResponse : Response
{
    public OkResponse() { }
}

public class OkResponse<T> : Response<T>
{
    public OkResponse(T data) : base(data) { }
}
