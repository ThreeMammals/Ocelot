using Ocelot.Responses;

namespace Ocelot.ScopedData
{
    public interface IScopedRequestDataRepository
    {
        Response Add<T>(string key, T value);
        Response<T> Get<T>(string key);
    }
}