using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Ocelot.Responses;
using Ocelot.Requester.Handler.Creator;
using Ocelot.Errors;

namespace Ocelot.Requester.Handler.Factory
{
    public class RequesterHandlerFactory : IRequesterHandlerFactory
    {
        private readonly IRequesterHandlerCreator _creator;

        public RequesterHandlerFactory(IRequesterHandlerCreator creator)
        {
            _creator = creator;
        }

        public Response<RequesterHandler> Get(IApplicationBuilder app, string schema)
        {
            var handler = _creator.Create(app, schema);

            if (!handler.IsError)
            {
                return new OkResponse<RequesterHandler>(
                    new RequesterHandler(schema, new RequestDelegateHandler(handler.Data)));
            }

            return new ErrorResponse<RequesterHandler>(new List<Error>
            {
                new UnableToCreateRequesterHandlerError($"Unable to create requester handler for {schema}")
            });
        }
    }
}
