using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Requester.Middleware;

namespace Ocelot.Requester.Handler.Creator
{
    public class RequesterHandlerCreator : IRequesterHandlerCreator
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
                    httpBuilder.UseHttpRequesterMiddleware();
                    requesterNext = httpBuilder.Build();
                    break;
            }
            return new OkResponse<RequestDelegate>(requesterNext);
        }
    }
}
