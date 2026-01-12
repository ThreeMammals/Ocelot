using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;

namespace Ocelot.AcceptanceTests.Logging;

public class TestLoggerFactory<TConsumer> : IOcelotLoggerFactory
{
    private readonly ILoggerFactory _factory;
    private readonly IRequestScopedDataRepository _repository;
    private readonly MemoryLogger _logger;
    private readonly OcelotLogger _ologger;

    public TestLoggerFactory(ILoggerFactory factory, IRequestScopedDataRepository repository)
    {
        _factory = factory;
        _repository = repository;
        _logger = new();
        _ologger = new(_logger, _repository);
    }

    public MemoryLogger Logger => _logger;
    public IOcelotLogger CreateLogger<TActualConsumer>() => _ologger;
}
