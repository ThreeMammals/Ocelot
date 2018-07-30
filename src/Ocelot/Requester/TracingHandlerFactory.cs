namespace Ocelot.Requester
{
    using System;
    using Butterfly.Client.Tracing;
    using Ocelot.Infrastructure.RequestData;
    using Microsoft.Extensions.DependencyInjection;

    public class TracingHandlerFactory : ITracingHandlerFactory
    {
        private readonly IServiceTracer _tracer;
        private readonly IRequestScopedDataRepository _repo;

        public TracingHandlerFactory(
            IServiceProvider services,
            IRequestScopedDataRepository repo)
        {
            _repo = repo;
            _tracer = services.GetService<IServiceTracer>();
        }

        public ITracingHandler Get()
        {
            return new OcelotHttpTracingHandler(_tracer, _repo);
        }
    }
}
