using System;
using AspectCore.Configuration;
using AspectCore.Extensions.DependencyInjection;
using Butterfly.Client.Logging;
using Butterfly.Client.Tracing;
using Butterfly.OpenTracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Butterfly.Client.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddButterfly(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddButterfly().Configure<ButterflyOptions>(configuration);
        }

        public static IServiceCollection AddButterfly(this IServiceCollection services, Action<ButterflyOptions> configure)
        {
            return services.AddButterfly().Configure<ButterflyOptions>(configure);
        }

        private static IServiceCollection AddButterfly(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTransient<HttpTracingHandler>();

            services.TryAddSingleton<ISpanContextFactory, SpanContextFactory>();
            services.TryAddSingleton<ISampler, FullSampler>();
            services.TryAddSingleton<ITracer, Tracer>();
            services.TryAddSingleton<IServiceTracerProvider, ServiceTracerProvider>();
            services.TryAddSingleton<ISpanRecorder, AsyncSpanRecorder>();
            services.TryAddSingleton<IButterflyDispatcherProvider, ButterflyDispatcherProvider>();
            services.TryAddSingleton<IButterflySenderProvider, ButterflySenderProvider>();
            services.TryAddSingleton<ITraceIdGenerator, TraceIdGenerator>();

            services.AddSingleton<IServiceTracer>(provider => provider.GetRequiredService<IServiceTracerProvider>().GetServiceTracer());
            services.AddSingleton<IButterflyDispatcher>(provider => provider.GetRequiredService<IButterflyDispatcherProvider>().GetDispatcher());       
            services.AddSingleton<IHostedService, ButterflyHostedService>();
            services.AddSingleton<ITracingDiagnosticListener, HttpRequestDiagnosticListener>();
            services.AddSingleton<ITracingDiagnosticListener, MvcTracingDiagnosticListener>();
            services.AddSingleton<IRequestTracer, RequestTracer>();
            services.AddSingleton<IDispatchCallback, SpanDispatchCallback>();     
            services.AddSingleton<ILoggerFactory, ButterflyLoggerFactory>();

            services.AddDynamicProxy(option =>
            {
                option.NonAspectPredicates.AddNamespace("Butterfly.*");
            });

            return services;
        }
    }
}