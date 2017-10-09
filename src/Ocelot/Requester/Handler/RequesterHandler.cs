using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Requester.Handler
{
    public class RequesterHandler
    {
        public RequesterHandler(string provider, IRequesterHandler handler)
        {
            Provider = provider;
            Handler = handler;
        }

        public string Provider { get; private set; }

        public IRequesterHandler Handler { get; private set; }
    }
}
