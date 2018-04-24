using BenchmarkDotNet.Running;

namespace Ocelot.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[] {
                    typeof(UrlPathToUrlPathTemplateMatcherBenchmarks),
                    typeof(AllTheThingsBenchmarks),
                    typeof(ExceptionHandlerMiddlewareBenchmarks)
               });

            switcher.Run(args);
        }
    }
}
