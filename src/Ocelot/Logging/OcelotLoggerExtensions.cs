using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Logging
{
    public static class OcelotLoggerExtensions
    {
        public static void TraceMiddlewareEntry(this IOcelotLogger logger)
        {
            logger.LogTrace($"entered {logger.Name}");
        }

        public static void TraceInvokeNext(this IOcelotLogger logger)
        {
            logger.LogTrace($"invoking next middleware from {logger.Name}");
        }

        public static void TraceInvokeNextCompleted(this IOcelotLogger logger)
        {
            logger.LogTrace($"returned to {logger.Name} after next middleware completed");
        }

        public static void TraceMiddlewareCompleted(this IOcelotLogger logger)
        {
            logger.LogTrace($"completed {logger.Name}");
        }
    }
}
