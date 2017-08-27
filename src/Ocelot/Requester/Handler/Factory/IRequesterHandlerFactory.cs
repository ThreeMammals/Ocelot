using Microsoft.AspNetCore.Builder;
using Ocelot.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Requester.Handler.Factory
{
    public interface IRequesterHandlerFactory
    {
        Response<RequesterHandler> Get(IApplicationBuilder app, string schema);
    }
}
