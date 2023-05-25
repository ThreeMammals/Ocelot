using Moq;

using Ocelot.Logging;
using Ocelot.Provider.Polly;
using Ocelot.Provider.Polly.Interfaces;

using Polly;
using Polly.Wrap;
using Shouldly;

using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Ocelot.UnitTests.Polly;

public class PollyCircuitBreakingDelegatingHandlerTests
{
    private readonly Mock<IPollyQoSProvider> pollyQoSProviderMock;
    private readonly Mock<IOcelotLoggerFactory> loggerFactoryMock;
    private readonly Mock<IOcelotLogger> loggerMock;

    private readonly PollyCircuitBreakingDelegatingHandler sut;

    public PollyCircuitBreakingDelegatingHandlerTests()
    {
        pollyQoSProviderMock = new Mock<IPollyQoSProvider>();

        loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        loggerMock = new Mock<IOcelotLogger>();

        loggerFactoryMock.Setup(x => x.CreateLogger<PollyCircuitBreakingDelegatingHandler>())
            .Returns(loggerMock.Object);
        loggerMock.Setup(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception>()));

        sut = new PollyCircuitBreakingDelegatingHandler(pollyQoSProviderMock.Object, loggerFactoryMock.Object);
    }

    [Fact]
    public async void SendAsync_OnePolicy_NoWrapping()
    {
        // Arrange
        var fakeResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        fakeResponse.Headers.Add("X-Xunit", nameof(SendAsync_OnePolicy_NoWrapping));

        MethodInfo method = null;
        var onePolicy = new Mock<IAsyncPolicy>();
        onePolicy.Setup(x => x.ExecuteAsync(It.IsAny<Func<Task<HttpResponseMessage>>>()))
            .Callback((IInvocation x) => method = x.Method)
            .ReturnsAsync(fakeResponse);

        pollyQoSProviderMock.SetupGet(x => x.CircuitBreaker)
            .Returns(new CircuitBreaker(onePolicy.Object));

        // Act
        var actual = await InvokeAsync("SendAsync");

        // Assert
        ShouldHaveXunitHeaderWithNoContent(actual, nameof(SendAsync_OnePolicy_NoWrapping));
        method.DeclaringType.Name.ShouldBe(nameof(IAsyncPolicy));
        method.DeclaringType.ShouldNotBeOfType<AsyncPolicyWrap>();
    }

    [Fact]
    public async void SendAsync_TwoPolicies_HaveWrapped()
    {
        // Arrange
        var fakeResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        fakeResponse.Headers.Add("X-Xunit", nameof(SendAsync_TwoPolicies_HaveWrapped));

        var policy1 = new FakeAsyncPolicy("Policy1", fakeResponse);
        var policy2 = new FakeAsyncPolicy("Policy2", fakeResponse);
        policy2.IsLast = true;

        pollyQoSProviderMock.SetupGet(x => x.CircuitBreaker)
            .Returns(new CircuitBreaker(policy1, policy2));

        // Act
        var actual = await InvokeAsync("SendAsync");

        // Assert
        ShouldHaveXunitHeaderWithNoContent(actual, nameof(SendAsync_TwoPolicies_HaveWrapped));
        ShouldBeWrappedBy(policy1, typeof(AsyncPolicyWrap).FullName);
        ShouldBeWrappedBy(policy2, typeof(AsyncPolicy).FullName);
    }

    private void ShouldHaveXunitHeaderWithNoContent(HttpResponseMessage actual, string headerName)
    {
        actual.ShouldNotBeNull();
        actual.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        actual.Headers.GetValues("X-Xunit").ShouldContain(headerName);
    }

    private void ShouldBeWrappedBy(FakeAsyncPolicy policy, string wrapperName)
    {
        policy.Called.ShouldBeTrue();
        policy.Times.ShouldBe(1);
        policy.Method.ShouldNotBeNull();
        policy.Target.ShouldNotBeNull();
        policy.Method.DeclaringType?.DeclaringType.ShouldNotBeNull();
        policy.Method.DeclaringType.DeclaringType.FullName.ShouldContain(wrapperName);
        policy.Target.ToString().ShouldContain(wrapperName);
    }

    private async Task<HttpResponseMessage> InvokeAsync(string methodName)
    {
        var m = typeof(PollyCircuitBreakingDelegatingHandler).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        var task = (Task<HttpResponseMessage>)m.Invoke(sut, new object[] { new HttpRequestMessage(), CancellationToken.None });
        var actual = await task;
        return actual;
    }

    internal class FakeAsyncPolicy : AsyncPolicy, IAsyncPolicy
    {
        public object Result { get; private set; }
        public string Name { get; private set; }

        public int Times { get; protected set; }
        public bool Called => Times > 0;
        public MethodInfo Method { get; protected set; }
        public object Target { get; protected set; }

        public bool IsLast { get; set; }

        public FakeAsyncPolicy(string name, object result)
        {
            Name = name;
            Result = result;
        }

        protected override async Task<TResult> ImplementationAsync<TResult>(Func<Context, CancellationToken, Task<TResult>> action,
            Context context, CancellationToken cancellationToken, bool continueOnCapturedContext)
        {
            Times++;
            Method = action.Method;
            Target = action.Target;

            if (IsLast)
            {
                TResult r = Result?.GetType() == typeof(TResult)
                    ? (TResult)Result
                    : Activator.CreateInstance<TResult>();
                return r;
            }

            TResult result = await action(context, cancellationToken);
            return result;
        }
    }
}
