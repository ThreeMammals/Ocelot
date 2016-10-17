namespace Ocelot.Library.Repository
{
    using Responses;

    public interface IScopedRequestDataRepository
    {
        Response Add<T>(string key, T value);
        Response<T> Get<T>(string key);
    }
}