﻿using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Headers;
using Ocelot.Responses;

namespace Ocelot.Responder
{
    using System.Collections.Generic;

    /// <summary>
    /// Cannot unit test things in this class due to methods not being implemented
    /// on .net concretes used for testing
    /// </summary>
    public class HttpContextResponder : IHttpResponder
    {
        private readonly IRemoveOutputHeaders _removeOutputHeaders;

        public HttpContextResponder(IRemoveOutputHeaders removeOutputHeaders)
        {
            _removeOutputHeaders = removeOutputHeaders;
        }

        public async Task SetResponseOnHttpContext(HttpContext context, HttpResponseMessage response)
        {
            _removeOutputHeaders.Remove(response.Headers);

            foreach (var httpResponseHeader in response.Headers)
            {
                AddHeaderIfDoesntExist(context, httpResponseHeader);
            }

            foreach (var httpResponseHeader in response.Content.Headers)
            {
                AddHeaderIfDoesntExist(context, httpResponseHeader);
            }

            var content = await response.Content.ReadAsByteArrayAsync();

            AddHeaderIfDoesntExist(context, new KeyValuePair<string, IEnumerable<string>>("Content-Length", new []{ content.Length.ToString() }) );

            context.Response.OnStarting(state =>
            {
                var httpContext = (HttpContext)state;

                httpContext.Response.StatusCode = (int)response.StatusCode;

                return Task.CompletedTask;

            }, context);

            using (Stream stream = new MemoryStream(content))
            {
                if (response.StatusCode != HttpStatusCode.NotModified)
                {
                    await stream.CopyToAsync(context.Response.Body);
                }
            }
        }

        private void AddHeaderIfDoesntExist(HttpContext context, KeyValuePair<string, IEnumerable<string>> httpResponseHeader)
        {
            if (!context.Response.Headers.ContainsKey(httpResponseHeader.Key))
            {
                context.Response.Headers.Add(httpResponseHeader.Key, new StringValues(httpResponseHeader.Value.ToArray()));
            }
        }

        public void SetErrorResponseOnContext(HttpContext context, int statusCode)
        {
            context.Response.OnStarting(x =>
            {
                context.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            }, context);
        }
    }
}