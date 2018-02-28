using System;
using AspectCore.DynamicProxy;

namespace Butterfly.Client.Logging
{
    public interface ILogger
    {
        void Info(string message);

        void Error(string message, Exception exception);
    }
}
