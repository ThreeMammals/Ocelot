using System;
using System.Threading;

namespace Butterfly.OpenTracing
{
    /// <summary>
    /// Thread-safe random long generator.
    ///
    /// See "Correct way to use Random in multithread application"
    /// http://stackoverflow.com/questions/19270507/correct-way-to-use-random-in-multithread-application
    /// </summary>
    public static class RandomUtils
    {
        private static int _seed = Guid.NewGuid().GetHashCode();

        [ThreadStatic]
        private static Random _localRandom;

        [ThreadStatic]
        private static byte[] _buffer;

        public static long NextLong()
        {
            EnsureInitialized();

            _localRandom.NextBytes(_buffer);
            var next = BitConverter.ToInt64(_buffer, 0);
            if (next < 0) next = Math.Abs(next);
            return next;
        }

        private static void EnsureInitialized()
        {
            if (_localRandom != null)
                return;
            _localRandom = new Random(Interlocked.Increment(ref _seed));
            _buffer = new byte[8];
        }
    }
}
