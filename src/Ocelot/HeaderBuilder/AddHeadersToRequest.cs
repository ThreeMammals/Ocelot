using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Library.Configuration;
using Ocelot.Library.HeaderBuilder.Parser;
using Ocelot.Library.Responses;

namespace Ocelot.Library.HeaderBuilder
{
    public class AddHeadersToRequest : IAddHeadersToRequest
    {
        private readonly IClaimsParser _claimsParser;

        public AddHeadersToRequest(IClaimsParser claimsParser)
        {
            _claimsParser = claimsParser;
        }

        public Response SetHeadersOnContext(List<ClaimToHeader> configurationHeaderExtractorProperties, HttpContext context)
        {
            foreach (var config in configurationHeaderExtractorProperties)
            {
                var value = _claimsParser.GetValue(context.User.Claims, config.ClaimKey, config.Delimiter, config.Index);

                if (value.IsError)
                {
                    return new ErrorResponse(value.Errors);
                }

                var exists = context.Request.Headers.FirstOrDefault(x => x.Key == config.HeaderKey);

                if (!string.IsNullOrEmpty(exists.Key))
                {
                    context.Request.Headers.Remove(exists);
                }

                context.Request.Headers.Add(config.HeaderKey, new StringValues(value.Data));
            }

            return new OkResponse();
        }
    }
}