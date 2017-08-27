using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Requester.Handler.Creator
{
    public interface IRequesterHandlerCreator
    {
        Response<RequestDelegate> Create(IApplicationBuilder app, string schema);
    }
}
