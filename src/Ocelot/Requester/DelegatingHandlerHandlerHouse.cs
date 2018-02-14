using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.Requester
{
    public class DelegatingHandlerHandlerHouse : IDelegatingHandlerHandlerHouse
    {
        private readonly IDelegatingHandlerHandlerProviderFactory _factory;
        private readonly ConcurrentDictionary<string, IDelegatingHandlerHandlerProvider> _housed;

        public DelegatingHandlerHandlerHouse(IDelegatingHandlerHandlerProviderFactory factory)
        {
            _factory = factory;
            _housed = new ConcurrentDictionary<string, IDelegatingHandlerHandlerProvider>();
        }

        public Response<IDelegatingHandlerHandlerProvider> Get(Request.Request request)
        {
            try
            {
                if (_housed.TryGetValue(request.ReRouteKey, out var provider))
                {
                    //todo once day we might need a check here to see if we need to create a new provider
                    provider = _housed[request.ReRouteKey];
                    return new OkResponse<IDelegatingHandlerHandlerProvider>(provider);
                }

                provider = _factory.Get(request);
                AddHoused(request.ReRouteKey, provider);
                return new OkResponse<IDelegatingHandlerHandlerProvider>(provider);
            }
            catch (Exception ex)
            {
                return new ErrorResponse<IDelegatingHandlerHandlerProvider>(new List<Error>()
                {
                    new UnableToFindDelegatingHandlerProviderError($"unabe to find delegating handler provider for {request.ReRouteKey} exception is {ex}")
                });
            }
        }

        private void AddHoused(string key, IDelegatingHandlerHandlerProvider provider)
        {
            _housed.AddOrUpdate(key, provider, (k, v) => provider);
        }
    }
}
