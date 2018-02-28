using System.Collections.Generic;

namespace Butterfly.OpenTracing
{
    public interface ISpanContextFactory
    {
        ISpanContext Create(SpanContextPackage spanContextPackage);
    }
}