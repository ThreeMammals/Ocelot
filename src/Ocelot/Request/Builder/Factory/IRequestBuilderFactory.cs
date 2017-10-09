using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Request.Builder.Factory
{
    public interface IRequestBuilderFactory
    {
        Response<RequestBuilder> Get(IApplicationBuilder app, string schema);
    }
}
