using System;
using AspectCore.DynamicProxy;

namespace Butterfly.Client.Logging
{
    public interface ILoggerFactory
    {
        ILogger CreateLogger(Type type);
    }
}
