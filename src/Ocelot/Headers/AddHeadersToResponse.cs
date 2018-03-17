namespace Ocelot.Headers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Ocelot.Configuration.Creator;
    using Ocelot.Infrastructure.RequestData;

    public class AddHeadersToResponse : IAddHeadersToResponse
    {
        private IRequestScopedDataRepository _repo;
        private Dictionary<string, Func<string>> _placeholders;

        public AddHeadersToResponse(IRequestScopedDataRepository repo)
        {
            _repo = repo;
             _placeholders = new Dictionary<string, Func<string>>();
            _placeholders.Add("{TraceId}", () => {
                var traceId = _repo.Get<string>("TraceId");
                return traceId.Data;
            });
        }
        public void Add(List<AddHeader> addHeaders, HttpResponseMessage response)
        {
            foreach(var add in addHeaders)
            {
                if(add.Value.StartsWith('{') && add.Value.EndsWith('}'))
                {
                    var handler = _placeholders[add.Value];
                    var value = handler();
                    response.Headers.TryAddWithoutValidation(add.Key, value);
                }
                else
                {
                    response.Headers.TryAddWithoutValidation(add.Key, add.Value);
                }
            }
        }
    }
}
