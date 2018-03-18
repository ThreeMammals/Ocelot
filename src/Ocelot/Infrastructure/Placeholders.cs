using System;
using System.Collections.Generic;
using System.Net.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.Infrastructure
{
    public class Placeholders : IPlaceholders
    {
        private Dictionary<string, Func<Response<string>>> _placeholders;
        private Dictionary<string, Func<HttpRequestMessage, string>> _requestPlaceholders;
        private readonly IBaseUrlFinder _finder;
        private readonly IRequestScopedDataRepository _repo;

        public Placeholders(IBaseUrlFinder finder, IRequestScopedDataRepository repo)
        {
            _repo = repo;
            _finder = finder;
            _placeholders = new Dictionary<string, Func<Response<string>>>();
            _placeholders.Add("{BaseUrl}", () => new OkResponse<string>(_finder.Find()));
            _placeholders.Add("{TraceId}", () => {
                var traceId = _repo.Get<string>("TraceId");
                if(traceId.IsError)
                {
                    return new ErrorResponse<string>(traceId.Errors);
                }

                return new OkResponse<string>(traceId.Data);
            });

            _requestPlaceholders = new Dictionary<string, Func<HttpRequestMessage, string>>();
            _requestPlaceholders.Add("{DownstreamBaseUrl}", x => {
                var downstreamUrl = $"{x.RequestUri.Scheme}://{x.RequestUri.Host}";

                if(x.RequestUri.Port != 80 && x.RequestUri.Port != 443)
                {
                    downstreamUrl = $"{downstreamUrl}:{x.RequestUri.Port}";
                }

                return $"{downstreamUrl}/";
            });
        }

        public Response<string> Get(string key)
        {
            if(_placeholders.ContainsKey(key))
            {
                var response = _placeholders[key].Invoke();
                if(!response.IsError)
                {
                    return new OkResponse<string>(response.Data);
                }
            }

            return new ErrorResponse<string>(new CouldNotFindPlaceholderError(key));
        }

        public Response<string> Get(string key, HttpRequestMessage request)
        {
            if(_requestPlaceholders.ContainsKey(key))
            {
                return new OkResponse<string>(_requestPlaceholders[key].Invoke(request));
            }

            return new ErrorResponse<string>(new CouldNotFindPlaceholderError(key));
        }
    }
}