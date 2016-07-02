namespace Ocelot.ApiGateway.Infrastructure.Responses
{
    public abstract class Error 
    {
        public Error(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}