using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public class AddHeadersToRequest : IAddHeadersToRequest
    {
        private readonly IClaimsParser _claimsParser;

        public AddHeadersToRequest(IClaimsParser claimsParser)
        {
            _claimsParser = claimsParser;
        }

        public Response SetHeadersOnContext(List<ClaimToThing> claimsToThings, HttpContext context)
        {
            foreach (var config in claimsToThings)
            {
                var value = _claimsParser.GetValue(context.User.Claims, config.NewKey, config.Delimiter, config.Index);

                if (value.IsError)
                {
                    return new ErrorResponse(value.Errors);
                }

                var exists = context.Request.Headers.FirstOrDefault(x => x.Key == config.ExistingKey);

                if (!string.IsNullOrEmpty(exists.Key))
                {
                    context.Request.Headers.Remove(exists);
                }

                context.Request.Headers.Add(config.ExistingKey, new StringValues(value.Data));
            }

            return new OkResponse();
        }
    }
}