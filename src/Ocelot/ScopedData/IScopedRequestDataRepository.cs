using Ocelot.Library.Responses;

namespace Ocelot.Library.ScopedData
{
    public interface IScopedRequestDataRepository
    {
        Response Add<T>(string key, T value);
        Response<T> Get<T>(string key);
    }
}