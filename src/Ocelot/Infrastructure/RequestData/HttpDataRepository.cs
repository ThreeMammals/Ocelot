using Microsoft.AspNetCore.Http;
using Ocelot.Responses;

namespace Ocelot.Infrastructure.RequestData
{
    public class HttpDataRepository : IRequestScopedDataRepository
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public HttpDataRepository(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public Response Add<T>(string key, T value)
        {
            try
            {
                _contextAccessor.HttpContext.Items.Add(key, value);
                return new OkResponse();
            }
            catch (Exception exception)
            {
                return new ErrorResponse(new CannotAddDataError(string.Format($"Unable to add data for key: {key}, exception: {exception.Message}")));
            }
        }

        public Response Update<T>(string key, T value)
        {
            try
            {
                _contextAccessor.HttpContext.Items[key] = value;
                return new OkResponse();
            }
            catch (Exception exception)
            {
                return new ErrorResponse(new CannotAddDataError(string.Format($"Unable to update data for key: {key}, exception: {exception.Message}")));
            }
        }

        public Response<T> Get<T>(string key)
        {
            if (_contextAccessor?.HttpContext?.Items == null)
            {
                return new ErrorResponse<T>(new CannotFindDataError($"Unable to find data for key: {key} because HttpContext or HttpContext.Items is null"));
            }

            return _contextAccessor.HttpContext.Items.TryGetValue(key, out var item)
                ? new OkResponse<T>((T)item)
                : new ErrorResponse<T>(new CannotFindDataError($"Unable to find data for key: {key}"));
        }
    }
}
