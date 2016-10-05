using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Repository
{
    public interface IScopedRequestDataRepository
    {
        Response Add<T>(string key, T value);
        Response<T> Get<T>(string key);
    }
}