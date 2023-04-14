namespace Ocelot.Requester
{
    using System;

    using Logging;

    using Microsoft.Extensions.DependencyInjection;

    using Infrastructure.RequestData;

    public class TracingHandlerFactory : ITracingHandlerFactory
    {
        private readonly ITracer _tracer;
        private readonly IRequestScopedDataRepository _repo;

        public TracingHandlerFactory(
            IServiceProvider services,
            IRequestScopedDataRepository repo)
        {
            _repo = repo;
            _tracer = services.GetService<ITracer>();
        }

        public ITracingHandler Get()
        {
            return new OcelotHttpTracingHandler(_tracer, _repo);
        }
    }
}
