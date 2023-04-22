namespace Ocelot.RequestId
{
    public class RequestId
    {
        public RequestId(string requestIdKey, string requestIdValue)
        {
            RequestIdKey = requestIdKey;
            RequestIdValue = requestIdValue;
        }

        public string RequestIdKey { get; }
        public string RequestIdValue { get; }
    }
}
