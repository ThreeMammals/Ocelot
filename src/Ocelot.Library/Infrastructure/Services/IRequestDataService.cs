using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Services
{
    public interface IRequestDataService
    {
        Response Add<T>(string key, T value);
        Response<T> Get<T>(string key);
    }
}