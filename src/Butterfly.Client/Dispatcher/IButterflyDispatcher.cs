using System;
using AspectCore.DynamicProxy;
using Butterfly.DataContract.Tracing;

namespace Butterfly.Client
{
    public interface IButterflyDispatcher : IDisposable
    {
        bool Dispatch(Span span);
    }
}