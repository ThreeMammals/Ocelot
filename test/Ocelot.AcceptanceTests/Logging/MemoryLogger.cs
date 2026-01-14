using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;

namespace Ocelot.AcceptanceTests.Logging;

public class MemoryLogger : ILogger
{
    public readonly ConcurrentQueue<string> _messages = new();
    public readonly ConcurrentQueue<Exception> _exceptions = new();

    public IReadOnlyCollection<string> Messages => _messages;
    public IReadOnlyCollection<Exception> Exceptions => _exceptions;
    public string Logbook => string.Join(Environment.NewLine, _messages);

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (state is null)
            return;

        var message = formatter?.Invoke(state, exception);
        if (message == null)
            return;

        if (exception is not null)
        {
            var builder = new StringBuilder()
                .AppendLine(message)
                .Append(exception.ToString());
            _messages.Enqueue(builder.ToString());
            _exceptions.Enqueue(exception);
        }
        else
        {
            _messages.Enqueue(message);
        }
    }
}
