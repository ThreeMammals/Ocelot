using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Request.Builder.Creator;
using Ocelot.Errors;

namespace Ocelot.Request.Builder.Factory
{
    public class RequestBuilderFactory : IRequestBuilderFactory
    {
        private readonly IRequestBuilderCreator _creator;

        public RequestBuilderFactory(IRequestBuilderCreator creator)
        {
            _creator = creator;
        }

        public Response<RequestBuilder> Get(IApplicationBuilder app, string schema)
        {
            var handler = _creator.Create(app, schema);

            if (!handler.IsError)
            {
                return new OkResponse<RequestBuilder>(
                    new RequestBuilder(schema, new RequestDelegateBuilder(handler.Data)));
            }

            return new ErrorResponse<RequestBuilder>(new List<Error>
            {
                new UnableToCreateRequestBuilderError($"Unable to create requester handler for {schema}")
            });
        }
    }
}
