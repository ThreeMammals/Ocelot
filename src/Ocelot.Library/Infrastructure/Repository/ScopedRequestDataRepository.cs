using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.Errors;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Repository
{
    public class ScopedRequestDataRepository : IScopedRequestDataRepository
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ScopedRequestDataRepository(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Response Add<T>(string key, T value)
        {
            try
            {
                _httpContextAccessor.HttpContext.Items.Add(key, value);
                return new OkResponse();
            }
            catch (Exception exception)
            {
                return new ErrorResponse(new List<Error>
                {
                    new CannotAddDataError(string.Format($"Unable to add data for key: {key}, exception: {exception.Message}"))
                });
            }
        }

        public Response<T> Get<T>(string key)
        {
            object obj;

            if(_httpContextAccessor.HttpContext.Items.TryGetValue(key, out obj))
            {
                var data = (T) obj;
                return new OkResponse<T>(data);
            }

            return new ErrorResponse<T>(new List<Error>
            {
                new CannotFindDataError($"Unable to find data for key: {key}")
            });
        } 
    }
}
