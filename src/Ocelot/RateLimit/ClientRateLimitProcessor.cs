﻿using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ocelot.RateLimit
{
    public class ClientRateLimitProcessor
    {
        private readonly IRateLimitCounterHandler _counterHandler;
        private readonly RateLimitCore _core;

        public ClientRateLimitProcessor(IRateLimitCounterHandler counterHandler)
        {
            _counterHandler = counterHandler;
            _core = new RateLimitCore(_counterHandler);
        }

        public RateLimitCounter ProcessRequest(ClientRequestIdentity requestIdentity, RateLimitOptions option)
        {
            return _core.ProcessRequest(requestIdentity, option);
        }

        public int RetryAfter(RateLimitCounter timestamp, RateLimitRule rule)
        {
            return _core.RetryAfter(timestamp, rule);
        }

        public RateLimitHeaders GetRateLimitHeaders(HttpContext context, ClientRequestIdentity requestIdentity, RateLimitOptions option)
        {
            return _core.GetRateLimitHeaders(context, requestIdentity, option);
        }
    }
}
