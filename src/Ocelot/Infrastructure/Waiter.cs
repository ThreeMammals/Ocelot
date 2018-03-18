using System;
using System.Diagnostics;

namespace Ocelot.Infrastructure
{
    public class Waiter
    {
        private readonly int _milliSeconds;

        public Waiter(int milliSeconds)
        {
            _milliSeconds = milliSeconds;
        }

        public bool Until(Func<bool> condition)
        {
            var stopwatch = Stopwatch.StartNew();
            var passed = false;
            while (stopwatch.ElapsedMilliseconds < _milliSeconds)
            {
                if (condition.Invoke())
                {
                    passed = true;
                    break;
                }
            }

            return passed;
        }

        public bool Until<T>(Func<bool> condition)
        {
            var stopwatch = Stopwatch.StartNew();
            var passed = false;
            while (stopwatch.ElapsedMilliseconds < _milliSeconds)
            {
                if (condition.Invoke())
                {
                    passed = true;
                    break;
                }
            }

            return passed;
        }
    }
}
