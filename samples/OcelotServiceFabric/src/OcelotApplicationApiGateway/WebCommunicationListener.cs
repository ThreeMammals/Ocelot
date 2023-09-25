// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System;
using System.Fabric;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace OcelotApplicationApiGateway
{
    public class WebCommunicationListener : ICommunicationListener
    {
        private readonly string _appRoot;
        private readonly ServiceContext _serviceInitializationParameters;
        private string _listeningAddress;
        private string _publishAddress;

        // OWIN server handle.
        private IWebHost _webHost;

        public WebCommunicationListener(string appRoot, ServiceContext serviceInitializationParameters)
        {
            _appRoot = appRoot;
            _serviceInitializationParameters = serviceInitializationParameters;
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.Message("Initialize");

            var serviceEndpoint = _serviceInitializationParameters.CodePackageActivationContext.GetEndpoint("WebEndpoint");
            var port = serviceEndpoint.Port;

            _listeningAddress = string.Format(
                CultureInfo.InvariantCulture,
                "http://+:{0}/{1}",
                port,
                string.IsNullOrWhiteSpace(_appRoot)
                    ? string.Empty
                    : _appRoot.TrimEnd('/') + '/');

            _publishAddress = _listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            ServiceEventSource.Current.Message("Starting web server on {0}", _listeningAddress);

            try
            {
                _webHost = new WebHostBuilder()
               .UseKestrel()
               .UseUrls(_listeningAddress)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddJsonFile("ocelot.json", false, false)
                        .AddEnvironmentVariables();
                })
               .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                })
                .ConfigureServices(s =>
                {
                    s.AddOcelot();
                })
                .Configure(a =>
                {
                    a.UseOcelot().Wait(cancellationToken);
                })
               .Build();

                _webHost.Start();
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceWebHostBuilderFailed(ex);
            }

            return Task.FromResult(_publishAddress);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            StopAll();
            return Task.FromResult(true);
        }

        public void Abort()
        {
            StopAll();
        }

        /// <summary>
        /// Stops, cancels, and disposes everything.
        /// </summary>
        private void StopAll()
        {
            try
            {
                if (_webHost != null)
                {
                    ServiceEventSource.Current.Message("Stopping web server.");
                    _webHost.Dispose();
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
