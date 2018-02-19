using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Provider;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.DownstreamRouteFinder.Middleware
{
    public delegate Task OcelotRequestDelegate(DownstreamContext downstreamContext);

    public class DownstreamContext
    {
        private ReRoute reRoute;
        private List<PlaceholderNameAndValue> templatePlaceholderNameAndValues;

        public DownstreamContext(HttpContext httpContext)
        {
            this.HttpContext = httpContext;
        }

        public DownstreamContext(HttpContext httpContext, ReRoute reRoute, List<PlaceholderNameAndValue> templatePlaceholderNameAndValues, ServiceProviderConfiguration serviceProviderConfiguration, DownstreamReRoute downstreamReRoute) : this(httpContext)
        {
            this.reRoute = reRoute;
            this.templatePlaceholderNameAndValues = templatePlaceholderNameAndValues;
            ServiceProviderConfiguration = serviceProviderConfiguration;
            DownstreamReRoute = downstreamReRoute;
        }

        public DownstreamRoute DownstreamRoute {get; set;}
        public ServiceProviderConfiguration ServiceProviderConfiguration {get; set;}
        public HttpContext HttpContext { get; set; }
        public DownstreamReRoute DownstreamReRoute { get; set; }
        public HttpRequestMessage DownstreamRequest { get; set; }
        public HttpResponseMessage DownstreamResponse { get; set; }
        public Request.Request Request { get; set; }
        public Ocelot.Responses.Response<DownstreamContext> Response { get;set; }
        public string RequestId {get;set;}
        public string PreviousRequestId {get;set;}
    }

    public class MultiplexerMiddleware : OcelotMiddlewareV2
    {
        private readonly OcelotRequestDelegate _realNext;
        private List<Thread> _threads;

        protected MultiplexerMiddleware(OcelotRequestDelegate realNext)
        {
            _realNext = realNext;
            _threads = new List<Thread>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            var tasks = new Task[context.DownstreamRoute.ReRoute.DownstreamReRoute.Count];

            for (int i = 0; i < context.DownstreamRoute.ReRoute.DownstreamReRoute.Count; i++)
            {
                //todo this is now a mess
                var downstreamContext = new DownstreamContext(context.HttpContext, context.DownstreamRoute.ReRoute, context.DownstreamRoute.TemplatePlaceholderNameAndValues, context.ServiceProviderConfiguration, context.DownstreamRoute.ReRoute.DownstreamReRoute[i]);

                tasks[i] = _realNext.Invoke(downstreamContext);
            }

            Task.WaitAll(tasks);

            //now cast the complete tasks to whatever they need to be
            //store them and let the response middleware handle them..
        }
    }

    public class DownstreamRouteFinderMiddleware : OcelotMiddlewareV2
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly IOcelotLogger _logger;
        private readonly IOcelotConfigurationProvider _configProvider;


        public DownstreamRouteFinderMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamRouteFinder downstreamRouteFinder,
            IOcelotConfigurationProvider configProvider)
        {
            _configProvider = configProvider;
            _next = next;
            _downstreamRouteFinder = downstreamRouteFinder;
            _logger = loggerFactory.CreateLogger<DownstreamRouteFinderMiddleware>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            var upstreamUrlPath = context.HttpContext.Request.Path.ToString();

            var upstreamHost = context.HttpContext.Request.Headers["Host"];

            var configuration = await _configProvider.Get();

            if (configuration.IsError)
            {
                _logger.LogError($"{MiddlewareName} setting pipeline errors. IOcelotConfigurationProvider returned {configuration.Errors.ToErrorString()}");
                SetPipelineError(context, configuration.Errors);
                return;
            }

            context.ServiceProviderConfiguration = configuration.Data.ServiceProviderConfiguration;

            _logger.LogDebug("upstream url path is {upstreamUrlPath}", upstreamUrlPath);

            var downstreamRoute = _downstreamRouteFinder.FindDownstreamRoute(upstreamUrlPath, context.HttpContext.Request.Method, configuration.Data, upstreamHost);

            if (downstreamRoute.IsError)
            {
                _logger.LogError($"{MiddlewareName} setting pipeline errors. IDownstreamRouteFinder returned {downstreamRoute.Errors.ToErrorString()}");

                SetPipelineError(context, downstreamRoute.Errors);
                return;
            }

            //todo - put this back in
            // _logger.LogDebug("downstream template is {downstreamRoute.Data.ReRoute.DownstreamPath}", downstreamRoute.Data.ReRoute.DownstreamReRoute.DownstreamPathTemplate);

            context.DownstreamRoute = downstreamRoute.Data;

            await _next.Invoke(context);
        }
    }
}
