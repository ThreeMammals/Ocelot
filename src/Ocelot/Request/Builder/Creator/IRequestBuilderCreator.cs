using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Request.Builder.Creator
{
    public interface IRequestBuilderCreator
    {
        Response<RequestDelegate> Create(IApplicationBuilder app, string schema);
    }
}
