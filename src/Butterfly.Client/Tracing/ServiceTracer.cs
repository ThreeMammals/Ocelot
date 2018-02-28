using System;
using Butterfly.OpenTracing;

namespace Butterfly.Client.Tracing
{
    public class ServiceTracer : IServiceTracer
    {
        private readonly ITracer _tracer;
        private readonly string _service;
        private readonly string _environment;
        private readonly string _identity;
        private readonly string _hostName;

        public ServiceTracer(ITracer tracer, string service, string environment, string identity, string hostName = null)
        {
            if (string.IsNullOrEmpty(service))
            {
                throw new ArgumentNullException(nameof(service));
            }
            if (string.IsNullOrEmpty(environment))
            {
                throw new ArgumentNullException(nameof(environment));
            }
            if (string.IsNullOrEmpty(identity))
            {
                throw new ArgumentNullException(nameof(identity));
            }
            if (tracer == null)
            {
                throw new ArgumentNullException(nameof(tracer));
            }
            _tracer = tracer;
            _service = service;
            _environment = environment;
            _identity = identity;
            _hostName = hostName;
        }

        public ITracer Tracer => _tracer;

        public string ServiceName => _service;

        public string Environment => _environment;

        public string Identity => _identity;

        public ISpan Start(ISpanBuilder spanBuilder)
        {
            var span = _tracer.Start(spanBuilder);

            span.Tags.Service(_service)
                .ServiceIdentity(_identity)
                .ServiceEnvironment(_environment)
                .ServiceHost(_hostName);

            return new ServiceSpan(span, _tracer);
        }
    }
}