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
    private bool _disposed;

    public TestLoggerFactory(ILoggerFactory factory, IRequestScopedDataRepository repository)
    {
        _factory = factory;
        _repository = repository;
        _logger = new();
        _ologger = new(_logger, _repository);
    }

    public MemoryLogger Logger => _logger;
    public IOcelotLogger CreateLogger<TActualConsumer>() => _ologger;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _factory?.Dispose();
        }

        _disposed = true;
    }

    ~TestLoggerFactory() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
