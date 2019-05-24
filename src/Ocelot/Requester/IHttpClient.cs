﻿using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ocelot.Requester
{
    public interface IHttpClient
    {
        HttpClient Client { get; }

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
    }
}
