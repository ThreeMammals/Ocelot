namespace Ocelot.Errors;

public class UnknownError : Error
{
    public UnknownError(string message)
        : base(message, OcelotErrorCode.UnknownError, 0)
    { }
}
