namespace Ocelot.Errors;

public abstract class Error
{
    protected Error(string message, OcelotErrorCode code, int statusCode)
    {
        HttpStatusCode = statusCode;
        Message = message;
        Code = code;
    }

    protected Error(Exception exception, OcelotErrorCode code, int statusCode)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Exception = exception;
        HttpStatusCode = statusCode;
        Message = exception.Message;
        Code = code;
    }

    public Exception Exception { get; }
    public string Message { get; }
    public OcelotErrorCode Code { get; }
    public int HttpStatusCode { get; }

    public override string ToString() => $"{Code}: {Message}";
}
