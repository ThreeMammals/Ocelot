using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspectCore.Configuration;
using AspectCore.DynamicProxy;
using AspectCore.Extensions.Reflection;
using Butterfly.OpenTracing;

namespace Butterfly.Client.Tracing
{
    public class TraceAttribute : AbstractInterceptorAttribute
    {
        private static readonly List<string> excepts = new List<string>
        {
            "Microsoft.Extensions.Logging",
            "Microsoft.Extensions.Options",
            "IServiceProvider",
            "IHttpContextAccessor",
            "ITelemetryInitializer",
            "IHostingEnvironment",
            "Autofac.*",
            "Autofac",
            "Butterfly.*"
        };

        public override async Task Invoke(AspectContext context, AspectDelegate next)
        {
            var serviceType = context.ServiceMethod.DeclaringType;
            if (excepts.Any(x => serviceType.Name.Matches(x)) || excepts.Any(x => serviceType.Namespace.Matches(x)) || context.Implementation is IServiceTracer)
            {
                await context.Invoke(next);
                return;
            }

            var serviceTracer = context.ServiceProvider.GetService(typeof(IServiceTracer)) as IServiceTracer;
            await serviceTracer?.ChildTraceAsync(context.ServiceMethod.GetReflector().DisplayName, DateTimeOffset.UtcNow, async span =>
               {
                   span.Log(LogField.CreateNew().MethodExecuting());
                   span.Tags.Set("ServiceType", context.ServiceMethod.DeclaringType.GetReflector().FullDisplayName);
                   span.Tags.Set("ImplementationType", context.ImplementationMethod.DeclaringType.GetReflector().FullDisplayName);
                   await context.Invoke(next);
                   span.Log(LogField.CreateNew().MethodExecuted());
               });
        }
    }
}