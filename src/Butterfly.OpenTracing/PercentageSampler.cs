using System;

namespace Butterfly.OpenTracing
{
    public class PercentageSampler : ISampler
    {
        private readonly Random _random = new Random();
        private readonly float _samplingRate;

        public PercentageSampler(float samplingRate)
        {
            _samplingRate = samplingRate;
        }

        public bool ShouldSample()
        {
            if (_samplingRate >= 100)
            {
                return true;
            }
            var random = _random.NextDouble();
            return random < _samplingRate;
        }
    }
}