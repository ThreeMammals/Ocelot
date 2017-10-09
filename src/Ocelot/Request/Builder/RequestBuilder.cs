using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Request.Builder
{
    public class RequestBuilder
    {
        public RequestBuilder(string provider, IRequestBuilder handler)
        {
            Provider = provider;
            Handler = handler;
        }

        public string Provider { get; private set; }

        public IRequestBuilder Handler { get; private set; }
    }
}
