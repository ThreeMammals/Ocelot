namespace Ocelot.Cache.CacheManager.Benchmarks
{
    using BenchmarkDotNet.Running;

    public class Program
    {
        public static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[] {
                    typeof(AllTheThingsBenchmarks),
               });

            switcher.Run(args);
        }
    }
}
