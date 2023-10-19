using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;
using Ocelot.Responses;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ocelot.Request.Mapper
{
    public class RequestMapper : IRequestMapper
    {
        private readonly string[] _unsupportedHeaders = { "host" };
        private readonly long? maxRequestBodySizeHttpsSys;
        private readonly long? maxRequestBodySizeKerstrelServer;
        private readonly long? maxRequestBodySizeIISServer;
        private IServer _server;
        public RequestMapper()
        {
        }

        public RequestMapper(IOptions<HttpSysOptions> httpSysOptions, IOptions<KestrelServerOptions> kerstrelServerOptions, IOptions<IISServerOptions> iISOptions,IServer server)
        {
            maxRequestBodySizeHttpsSys = httpSysOptions.Value.MaxRequestBodySize.GetValueOrDefault();
            maxRequestBodySizeKerstrelServer = kerstrelServerOptions.Value.Limits.MaxRequestBodySize;
            maxRequestBodySizeIISServer = iISOptions.Value.MaxRequestBodySize;
            _server = server;
        }

        public async Task<Response<HttpRequestMessage>> Map(HttpRequest request, DownstreamRoute downstreamRoute)
        {
            try
            {
                var requestMessage = new HttpRequestMessage
                {
                    Content = await MapContent(request),
                    Method = MapMethod(request, downstreamRoute),
                    RequestUri = MapUri(request),
                    Version = downstreamRoute.DownstreamHttpVersion,
                };

                MapHeaders(request, requestMessage);

                return new OkResponse<HttpRequestMessage>(requestMessage);
            }
            catch (Exception ex)
            {
                if (PayloadTooLargeOnAnyHostedServer(request, ex))
                {
                    return new ErrorResponse<HttpRequestMessage>(new PayloadTooLargeError(ex));
                }

                return new ErrorResponse<HttpRequestMessage>(new UnmappableRequestError(ex));
            }
        }

        private bool PayloadTooLargeOnAnyHostedServer(HttpRequest request, Exception ex)
        {
            bool isHeavyPayload = false;

            if(_server is KestrelServer)
            {
                isHeavyPayload = maxRequestBodySizeKerstrelServer < request.ContentLength;
            }
            else if (IsRunningInProcessIIS())
            {
               isHeavyPayload = maxRequestBodySizeIISServer < request.ContentLength;
            }
            else
            {
                isHeavyPayload = maxRequestBodySizeHttpsSys < request.ContentLength && RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            }

            return isHeavyPayload && ex is Microsoft.AspNetCore.Http.BadHttpRequestException;
        }

        /// <summary>
        /// Check if this process is running on Windows in an in process instance in IIS.
        /// </summary>
        /// <returns>True if Windows and in an in process instance on IIS, false otherwise.</returns>
        public static bool IsRunningInProcessIIS()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            string processName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName);
            return processName.Contains("w3wp", StringComparison.OrdinalIgnoreCase) ||
                processName.Contains("iisexpress", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<HttpContent> MapContent(HttpRequest request)
        {
            if (request.Body == null || (request.Body.CanSeek && request.Body.Length <= 0))
            {
                return null;
            }

            // Never change this to StreamContent again, I forgot it doesnt work in #464.
            var content = new ByteArrayContent(await ToByteArray(request.Body));

            if (!string.IsNullOrEmpty(request.ContentType))
            {
                content.Headers
                    .TryAddWithoutValidation("Content-Type", new[] { request.ContentType });
            }

            AddHeaderIfExistsOnRequest("Content-Language", content, request);
            AddHeaderIfExistsOnRequest("Content-Location", content, request);
            AddHeaderIfExistsOnRequest("Content-Range", content, request);
            AddHeaderIfExistsOnRequest("Content-MD5", content, request);
            AddHeaderIfExistsOnRequest("Content-Disposition", content, request);
            AddHeaderIfExistsOnRequest("Content-Encoding", content, request);

            return content;
        }

        private static void AddHeaderIfExistsOnRequest(string key, HttpContent content, HttpRequest request)
        {
            if (request.Headers.ContainsKey(key))
            {
                content.Headers
                    .TryAddWithoutValidation(key, request.Headers[key].ToArray());
            }
        }

        private static HttpMethod MapMethod(HttpRequest request, DownstreamRoute downstreamRoute)
        {
            if (!string.IsNullOrEmpty(downstreamRoute?.DownstreamHttpMethod))
            {
                return new HttpMethod(downstreamRoute.DownstreamHttpMethod);
            }

            return new HttpMethod(request.Method);
        }

        private static Uri MapUri(HttpRequest request) => new(request.GetEncodedUrl());

        private void MapHeaders(HttpRequest request, HttpRequestMessage requestMessage)
        {
            foreach (var header in request.Headers)
            {
                if (IsSupportedHeader(header))
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }

        private bool IsSupportedHeader(KeyValuePair<string, StringValues> header)
        {
            return !_unsupportedHeaders.Contains(header.Key.ToLower());
        }

        private static async Task<byte[]> ToByteArray(Stream stream)
        {
            await using (stream)
            {
                using (var memStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memStream);
                    return memStream.ToArray();
                }
            }
        }
    }
}
