using System.Collections.Generic;
using System.Linq;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Provider.Polly
{
    public class CircuitBreaker
    {
        private readonly List<IAsyncPolicy> _policies = new List<IAsyncPolicy>();
        
        public CircuitBreaker(params IAsyncPolicy[] policies)
        {
            foreach (var policy in policies.Where(p => p != null))
            {
                this._policies.Add(policy);
            }
        }
        
        public IAsyncPolicy[] Policies => this._policies.ToArray();
    }
}
