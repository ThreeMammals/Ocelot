namespace Ocelot.Errors
{
    public abstract class Error
    {
        protected Error(string message, OcelotErrorCode code, int httpStatusCode)
        {
            HttpStatusCode = httpStatusCode;
            Message = message;
            Code = code;
        }

        public string Message { get; }
        public OcelotErrorCode Code { get; }
        public int HttpStatusCode { get; }

        public override string ToString()
        {
            return Message;
        }
    }
}
