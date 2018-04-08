namespace Ocelot.Headers
{
    using System.Collections.Generic;
    using System.Net.Http;
    using Configuration.Creator;
    using Infrastructure;
    using Logging;

    public class AddHeadersToResponse : IAddHeadersToResponse
    {
        private readonly IPlaceholders _placeholders;
        private readonly IOcelotLogger _logger;

        public AddHeadersToResponse(IPlaceholders placeholders, IOcelotLoggerFactory factory)
        {
            _logger = factory.CreateLogger<AddHeadersToResponse>();
            _placeholders = placeholders;
        }

        public void Add(List<AddHeader> addHeaders, HttpResponseMessage response)
        {
            foreach(var add in addHeaders)
            {
                if(add.Value.StartsWith('{') && add.Value.EndsWith('}'))
                {
                    var value = _placeholders.Get(add.Value);
                    
                    if(value.IsError)
                    {
                        _logger.LogWarning($"Unable to add header to response {add.Key}: {add.Value}");
                        continue;
                    }

                    response.Headers.TryAddWithoutValidation(add.Key, value.Data);
                }
                else
                {
                    response.Headers.TryAddWithoutValidation(add.Key, add.Value);
                }
            }
        }
    }
}
