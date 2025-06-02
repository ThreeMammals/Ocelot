using Microsoft.Reactive.Testing;
using Ocelot.Provider.Kubernetes;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Ocelot.UnitTests.Kubernetes
{
    public class ObservableExtensionsTests
    {
        private readonly TestScheduler _testScheduler = new();
        
        [Fact]
        public async Task RetryAfter_ExceptionThrown_RetriesInfiniteWithDelay()
        {
            // Arrange
            var errorsToThrow = Random.Shared.Next(10, 1000);
            var errorsCounter = 0;
            var expectedResult = 123;
            var delaySeconds = TimeSpan.FromSeconds(3);
            var observable = Observable.Create<int>(observer =>
            {
                if (errorsCounter < errorsToThrow)
                {
                    errorsCounter++;
                    throw new Exception("Need to catch and retry");
                }

                observer.OnNext(expectedResult);
                return Disposable.Empty;
            });
            
            // Act
            using var cts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                // have to spin in separate thread because it is used after first subscription and stops after first Exception
                while (!cts.Token.IsCancellationRequested)
                    _testScheduler.Start();
            });
            
            var result = await observable.RetryAfter(delaySeconds, _testScheduler).FirstAsync();
            await cts.CancelAsync();
            
            // Assert
            result.ShouldBe(expectedResult);
            errorsCounter.ShouldBe(errorsToThrow);
            _testScheduler.Clock.ShouldBe(delaySeconds.Ticks * errorsToThrow);
        }
    }
}
