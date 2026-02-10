using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;

namespace Ocelot.Logging;

public class OcelotLoggerFactory : IOcelotLoggerFactory, IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IRequestScopedDataRepository _scopedDataRepository;
    private bool _disposed;

    public OcelotLoggerFactory(ILoggerFactory loggerFactory, IRequestScopedDataRepository scopedDataRepository)
    {
        _loggerFactory = loggerFactory;
        _scopedDataRepository = scopedDataRepository;
    }

    public IOcelotLogger CreateLogger<T>()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var logger = _loggerFactory.CreateLogger<T>();
        return new OcelotLogger(logger, _scopedDataRepository);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _loggerFactory?.Dispose();
        }

        _disposed = true;
    }

    ~OcelotLoggerFactory() => Dispose(false);
}
