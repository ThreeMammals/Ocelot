using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Requester.Handler;
using Ocelot.Request.Middleware;

namespace Ocelot.Request.Builder.Creator
{
    public class RequestBuilderCreator : IRequestBuilderCreator
    {
        public Response<RequestDelegate> Create(IApplicationBuilder app, string schema)
        {
            RequestDelegate requesterNext = null;

            string provider = schema;
            SupportedRequesterProviders supportedProvider;
            bool isSupportedRequesterProvider = Enum.TryParse(provider, true, out supportedProvider);
            switch (supportedProvider)
            {
                case SupportedRequesterProviders.Http:
                case SupportedRequesterProviders.Https:
                    var httpBuilder = app.New();
                    httpBuilder.UseHttpRequestBuilderMiddleware();
                    requesterNext = httpBuilder.Build();
                    break;
            }
            return new OkResponse<RequestDelegate>(requesterNext);
        }
    }
}
