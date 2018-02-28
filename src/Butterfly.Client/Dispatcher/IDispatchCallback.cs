using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspectCore.DynamicProxy;

namespace Butterfly.Client
{
    [NonAspect]
    public interface IDispatchCallback
    {
        Func<DispatchableToken, bool> Filter { get; }

        Task Accept(IEnumerable<IDispatchable> dispatchables);
    }
}
