using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Ocelot.Provider.Kubernetes
{
    public static class ObservableExtensions
    {
        public static IObservable<TSource> RetryAfter<TSource>(this IObservable<TSource> source,
            TimeSpan dueTime,
            IScheduler scheduler)
        {
            return RepeatInfinite(source, dueTime, scheduler).Catch();
        }

        private static IEnumerable<IObservable<TSource>> RepeatInfinite<TSource>(IObservable<TSource> source,
            TimeSpan dueTime,
            IScheduler scheduler)
        {
            yield return source;

            while (true)
            {
                yield return source.DelaySubscription(dueTime, scheduler);
            }
        }
    }
}
