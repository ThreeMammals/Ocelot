using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;

namespace Ocelot.Logging
{
    public class OcelotLoggerFactory : IOcelotLoggerFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IRequestScopedDataRepository _scopedDataRepository;

        public OcelotLoggerFactory(ILoggerFactory loggerFactory, IRequestScopedDataRepository scopedDataRepository)
        {
            _loggerFactory = loggerFactory;
            _scopedDataRepository = scopedDataRepository;
        }

        public IOcelotLogger CreateLogger<T>()
        {
            var logger = _loggerFactory.CreateLogger<T>();
            return new OcelotLogger(logger, _scopedDataRepository);
        }
    }
}
