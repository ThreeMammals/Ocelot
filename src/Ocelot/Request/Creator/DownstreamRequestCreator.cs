namespace Ocelot.Request.Creator
{
    using System.Net.Http;

    using Infrastructure;

    using Middleware;

    public class DownstreamRequestCreator : IDownstreamRequestCreator
    {
        private readonly IFrameworkDescription _framework;
        private const string DotNetFramework = ".NET Framework";

        public DownstreamRequestCreator(IFrameworkDescription framework)
        {
            _framework = framework;
        }

        /**
             * According to https://tools.ietf.org/html/rfc7231
             * GET,HEAD,DELETE,CONNECT,TRACE
             * Can have body but server can reject the request.
             * And MS HttpClient in Full Framework actually rejects it.
             * see #366 issue
         **/
        public DownstreamRequest Create(HttpRequestMessage request)
        {
            if (_framework.Get().Contains(DotNetFramework))
            {
                if (request.Method == HttpMethod.Get ||
                    request.Method == HttpMethod.Head ||
                    request.Method == HttpMethod.Delete ||
                    request.Method == HttpMethod.Trace)
                {
                    request.Content = null;
                }
            }

            return new DownstreamRequest(request);
        }
    }
}
