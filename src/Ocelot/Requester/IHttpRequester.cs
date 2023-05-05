﻿namespace Ocelot.Requester
{
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;

    using Responses;

    public interface IHttpRequester
    {
        Task<Response<HttpResponseMessage>> GetResponse(HttpContext httpContext);
    }
}
