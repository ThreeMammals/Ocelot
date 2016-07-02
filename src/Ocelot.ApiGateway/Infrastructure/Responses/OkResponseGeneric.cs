namespace Ocelot.ApiGateway.Infrastructure.Responses
{
    public class OkResponse<T> : Response<T>
    {
        public OkResponse(T data) : base(data)
        {
        }
    }
}