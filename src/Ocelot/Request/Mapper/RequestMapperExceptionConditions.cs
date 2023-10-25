using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ocelot.Request.Mapper
{
    public class RequestMapperExceptionConditions : IRequestMapperExceptionConditions
    {
        private readonly long? maxRequestBodySizeHttpsSys;
        private readonly long? maxRequestBodySizeKerstrelServer;
        private readonly long? maxRequestBodySizeIISServer;
        private IServer _server;
        public RequestMapperExceptionConditions(IOptions<HttpSysOptions> httpSysOptions, IOptions<KestrelServerOptions> kerstrelServerOptions, IOptions<IISServerOptions> iISOptions, IServer server)
        {
            maxRequestBodySizeHttpsSys = httpSysOptions.Value.MaxRequestBodySize.GetValueOrDefault();
            maxRequestBodySizeKerstrelServer = kerstrelServerOptions.Value.Limits.MaxRequestBodySize;
            maxRequestBodySizeIISServer = iISOptions.Value.MaxRequestBodySize;
            _server = server;
        }

        /// <summary>
        /// Check if this process is running on Windows in an in process instance in IIS.
        /// </summary>
        /// <returns>True if Windows and in an in process instance on IIS, false otherwise.</returns>
        private static bool IsRunningInProcessIIS()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            string processName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName);
            return processName.Contains("w3wp", StringComparison.OrdinalIgnoreCase) ||
                processName.Contains("iisexpress", StringComparison.OrdinalIgnoreCase);
        }

        public bool PayloadTooLargeOnAnyHostedServer(HttpRequest request, Exception ex)
        {
            bool isHeavyPayload = false;

            if (_server is KestrelServer)
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
    }
}
