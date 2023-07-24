using Polly;
using System.Collections.Generic;
using System.Linq;

namespace Ocelot.Provider.Polly
{
    public class CircuitBreaker
    {
        private readonly List<IAsyncPolicy> _policies = new();

        public CircuitBreaker(params IAsyncPolicy[] policies)
        {
            foreach (var policy in policies.Where(p => p != null))
            {
                _policies.Add(policy);
            }
        }

        public IAsyncPolicy[] Policies => _policies.ToArray();
    }
}
