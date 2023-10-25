using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ocelot.Request.Mapper
{
    public class RequestMapperExceptionConditions : IRequestMapperExceptionConditions
    {
        private readonly long? _maxRequestBodySizeHttpsSys;
        private readonly long? _maxRequestBodySizeKerstrelServer;
        private readonly long? _maxRequestBodySizeIISServer;
        private IServiceProvider _serviceProvider;
        public RequestMapperExceptionConditions(IOptions<HttpSysOptions> httpSysOptions, IOptions<KestrelServerOptions> kerstrelServerOptions, IOptions<IISServerOptions> iISOptions, IServiceProvider serviceProvider)
        {
            _maxRequestBodySizeHttpsSys = httpSysOptions.Value.MaxRequestBodySize.GetValueOrDefault();
            _maxRequestBodySizeKerstrelServer = kerstrelServerOptions.Value.Limits.MaxRequestBodySize;
            _maxRequestBodySizeIISServer = iISOptions.Value.MaxRequestBodySize;
            _serviceProvider = serviceProvider;
        }

        public bool PayloadTooLargeOnAnyHostedServer(HttpRequest request, Exception ex)
        {
            var server = _serviceProvider.GetRequiredService<IServer>();
            const string iisServiceName = "W3SVC";

            if (server != null && ex is Microsoft.AspNetCore.Http.BadHttpRequestException)
            {
                var serverName = server.GetType().FullName;

                switch (serverName)
                {
                    case "Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServer":
                        return _maxRequestBodySizeKerstrelServer < request.ContentLength;
                    case iisServiceName:
                        return _maxRequestBodySizeIISServer < request.ContentLength;
                    default:
                        return _maxRequestBodySizeHttpsSys < request.ContentLength && RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                }
            }

            return false;
        }
    }
}
