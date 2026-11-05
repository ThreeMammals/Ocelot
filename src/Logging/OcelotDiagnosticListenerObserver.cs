using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;
using System.Reflection;

namespace Ocelot.Logging;

internal sealed class OcelotDiagnosticListenerObserver : IObserver<KeyValuePair<string, object>>
{
    private const string MiddlewareStarting = "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareStarting";
    private const string MiddlewareException = "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException";
    private const string MiddlewareFinished = "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareFinished";

    private static readonly ConcurrentDictionary<(Type PayloadType, string PropertyName), Func<object, object>> PropertyGetters = new();

    private readonly OcelotDiagnosticListener _listener;

    public OcelotDiagnosticListenerObserver(OcelotDiagnosticListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _listener = listener;
    }

    public static bool IsMiddlewareAnalysisEvent(string eventName)
        => eventName is MiddlewareStarting or MiddlewareException or MiddlewareFinished;

    public void OnNext(KeyValuePair<string, object> diagnosticEvent)
    {
        var payload = diagnosticEvent.Value;
        switch (diagnosticEvent.Key)
        {
            case MiddlewareStarting
                when TryGetValue(payload, "httpContext", out HttpContext context) && TryGetValue(payload, "name", out string name):
                _listener.OnMiddlewareStarting(context, name);
                break;
            case MiddlewareException
                when TryGetValue(payload, "exception", out Exception exception) && TryGetValue(payload, "name", out string name):
                _listener.OnMiddlewareException(exception, name);
                break;
            case MiddlewareFinished
                when TryGetValue(payload, "httpContext", out HttpContext context) && TryGetValue(payload, "name", out string name):
                _listener.OnMiddlewareFinished(context, name);
                break;
        }
    }

    public void OnError(Exception error)
    {
        // Diagnostic failures must not affect request processing.
    }

    public void OnCompleted()
    {
        // The application-lifetime subscription has no completion work.
    }

    private static bool TryGetValue<T>(object payload, string propertyName, out T value)
    {
        value = default;
        if (payload is null)
        {
            return false;
        }

        var getter = PropertyGetters.GetOrAdd(
            (payload.GetType(), propertyName),
            static key => CreateGetter(key.PayloadType, key.PropertyName));
        if (getter(payload) is T typedValue)
        {
            value = typedValue;
            return true;
        }

        return false;
    }

    private static Func<object, object> CreateGetter(Type payloadType, string propertyName)
    {
        var property = payloadType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (property is null)
        {
            return static _ => null;
        }

        var payload = Expression.Parameter(typeof(object), "payload");
        var typedPayload = Expression.Convert(payload, payloadType);
        var propertyValue = Expression.Property(typedPayload, property);
        var boxedValue = Expression.Convert(propertyValue, typeof(object));
        return Expression.Lambda<Func<object, object>>(boxedValue, payload).Compile();
    }
}