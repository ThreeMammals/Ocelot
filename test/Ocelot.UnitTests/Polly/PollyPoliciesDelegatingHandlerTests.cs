using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Polly;
using Ocelot.Provider.Polly.Interfaces;
using Polly;
using Polly.Wrap;
using System.Reflection;

namespace Ocelot.UnitTests.Polly;

public class PollyPoliciesDelegatingHandlerTests
{
    private readonly Mock<IPollyQoSResiliencePipelineProvider<HttpResponseMessage>> _pollyQoSResiliencePipelineProviderMock;
    private readonly Mock<IHttpContextAccessor> _contextAccessorMock;
    private readonly PollyPoliciesDelegatingHandler _sut;

    public PollyPoliciesDelegatingHandlerTests()
    {
        this._pollyQoSResiliencePipelineProviderMock = new Mock<IPollyQoSResiliencePipelineProvider<HttpResponseMessage>>();

        var loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        var loggerMock = new Mock<IOcelotLogger>();
        _contextAccessorMock = new Mock<IHttpContextAccessor>();

        loggerFactoryMock.Setup(x => x.CreateLogger<PollyPoliciesDelegatingHandler>())
            .Returns(loggerMock.Object);
        loggerMock.Setup(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception>()));

        _sut = new PollyPoliciesDelegatingHandler(DownstreamRouteFactory(), _contextAccessorMock.Object, loggerFactoryMock.Object);
    }

    [Fact]
    public async void SendAsync_OnePolicy_NoWrapping()
    {
        // Arrange
        var fakeResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        fakeResponse.Headers.Add("X-Xunit", nameof(SendAsync_OnePolicy_NoWrapping));

        MethodInfo method = null;
        var resiliencePipeline = new Mock<ResiliencePipeline<HttpResponseMessage>>();
        resiliencePipeline.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, ValueTask<HttpResponseMessage>>>(),CancellationToken.None))
            .Callback((Func<Context, CancellationToken, Task<HttpResponseMessage>> action) =>
            {
                method = action.Method;
            })
            .ReturnsAsync(fakeResponse);

        this._pollyQoSResiliencePipelineProviderMock.Setup(x => x.GetResiliencePipeline(It.IsAny<DownstreamRoute>()))
            .Returns(resiliencePipeline.Object);

     
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.RequestServices.GetService(typeof(IPollyQoSProvider<HttpResponseMessage>)))
            .Returns(this._pollyQoSResiliencePipelineProviderMock.Object);

        _contextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext.Object);

        // Act
        var actual = await InvokeAsync("SendAsync");

        // Assert
        ShouldHaveXunitHeaderWithNoContent(actual, nameof(SendAsync_OnePolicy_NoWrapping));
        method.DeclaringType.Name.ShouldBe("IAsyncPolicy`1");
        method.DeclaringType.ShouldNotBeOfType<AsyncPolicyWrap>();
    }


    // deprecated because there is no more wrapping policies in polly 8
    //[Fact]
    //public async void SendAsync_TwoPolicies_HaveWrapped()
    //{
    //    // Arrange
    //    var fakeResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
    //    fakeResponse.Headers.Add("X-Xunit", nameof(SendAsync_TwoPolicies_HaveWrapped));

    //    var policy1 = new FakeAsyncPolicy<HttpResponseMessage>("Policy1", fakeResponse);
    //    var policy2 = new FakeAsyncPolicy<HttpResponseMessage>("Policy2", fakeResponse)
    //    {
    //        IsLast = true,
    //    };

    //    _pollyQoSProviderMock.Setup(x => x.GetPollyPolicyWrapper(It.IsAny<DownstreamRoute>()))
    //        .Returns(new PollyPolicyWrapper<HttpResponseMessage>(policy1, policy2));

    //    var httpContext = new Mock<HttpContext>();
    //    httpContext.Setup(x => x.RequestServices.GetService(typeof(IPollyQoSProvider<HttpResponseMessage>)))
    //        .Returns(_pollyQoSProviderMock.Object);

    //    _contextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext.Object);

    //    // Act
    //    var actual = await InvokeAsync("SendAsync");

    //    // Assert
    //    ShouldHaveXunitHeaderWithNoContent(actual, nameof(SendAsync_TwoPolicies_HaveWrapped));
    //    ShouldBeWrappedBy(policy1, typeof(AsyncPolicyWrap).FullName);
    //    ShouldBeWrappedBy(policy2, typeof(AsyncPolicy).FullName);
    //}

    private static DownstreamRoute DownstreamRouteFactory()
    {
        var options = new QoSOptionsBuilder()
            .WithTimeoutValue(100)
            .WithExceptionsAllowedBeforeBreaking(2)
            .WithDurationOfBreak(200)
            .Build();

        var upstreamPath = new UpstreamPathTemplateBuilder()
            .WithTemplate("/")
            .WithContainsQueryString(false)
            .WithPriority(1)
            .WithOriginalValue("/").Build();

        var route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .WithUpstreamPathTemplate(upstreamPath).Build();

        return route;
    }

    private static void ShouldHaveXunitHeaderWithNoContent(HttpResponseMessage actual, string headerName)
    {
        actual.ShouldNotBeNull();
        actual.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        actual.Headers.GetValues("X-Xunit").ShouldContain(headerName);
    }

    private static void ShouldBeWrappedBy(FakeAsyncPolicy<HttpResponseMessage> policy, string wrapperName)
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
        var m = typeof(PollyPoliciesDelegatingHandler).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        var task = (Task<HttpResponseMessage>)m.Invoke(_sut, new object[] { new HttpRequestMessage(), CancellationToken.None });
        var actual = await task;
        return actual;
    }

    internal class FakeAsyncPolicy<TResult> : AsyncPolicy<TResult>, IAsyncPolicy
        where TResult : class
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

        protected override async Task<TResult> ImplementationAsync(Func<Context, CancellationToken, Task<TResult>> action, Context context, CancellationToken cancellationToken,
            bool continueOnCapturedContext)
        {
            Times++;
            Method = action.Method;
            Target = action.Target;

            if (IsLast)
            {
                var r = Result?.GetType() == typeof(TResult)
                    ? (TResult)Result
                    : Activator.CreateInstance<TResult>();
                return r;
            }

            var result = await action(context, cancellationToken);
            return result;
        }

        public new IAsyncPolicy WithPolicyKey(string policyKey) => throw new NotImplementedException();

        public Task ExecuteAsync(Func<Task> action) => throw new NotImplementedException();

        public Task ExecuteAsync(Func<Context, Task> action, IDictionary<string, object> contextData) => throw new NotImplementedException();

        public Task ExecuteAsync(Func<Context, Task> action, Context context) => throw new NotImplementedException();

        public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task ExecuteAsync(Func<Context, CancellationToken, Task> action, IDictionary<string, object> contextData, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task ExecuteAsync(Func<Context, CancellationToken, Task> action, Context context, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken, bool continueOnCapturedContext) => throw new NotImplementedException();

        public Task ExecuteAsync(Func<Context, CancellationToken, Task> action, IDictionary<string, object> contextData, CancellationToken cancellationToken, bool continueOnCapturedContext) => throw new NotImplementedException();

        public Task ExecuteAsync(Func<Context, CancellationToken, Task> action, Context context, CancellationToken cancellationToken, bool continueOnCapturedContext) => throw new NotImplementedException();

        public Task<TResult1> ExecuteAsync<TResult1>(Func<Task<TResult1>> action) => throw new NotImplementedException();

        public Task<TResult1> ExecuteAsync<TResult1>(Func<Context, Task<TResult1>> action, Context context) => throw new NotImplementedException();

        public Task<TResult1> ExecuteAsync<TResult1>(Func<Context, Task<TResult1>> action, IDictionary<string, object> contextData) => throw new NotImplementedException();

        public Task<TResult1> ExecuteAsync<TResult1>(Func<CancellationToken, Task<TResult1>> action, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<TResult1> ExecuteAsync<TResult1>(Func<Context, CancellationToken, Task<TResult1>> action, IDictionary<string, object> contextData, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<TResult1> ExecuteAsync<TResult1>(Func<Context, CancellationToken, Task<TResult1>> action, Context context, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<TResult1> ExecuteAsync<TResult1>(Func<CancellationToken, Task<TResult1>> action, CancellationToken cancellationToken, bool continueOnCapturedContext) => throw new NotImplementedException();

        public Task<TResult1> ExecuteAsync<TResult1>(Func<Context, CancellationToken, Task<TResult1>> action, IDictionary<string, object> contextData, CancellationToken cancellationToken, bool continueOnCapturedContext) => throw new NotImplementedException();

        public Task<TResult1> ExecuteAsync<TResult1>(Func<Context, CancellationToken, Task<TResult1>> action, Context context, CancellationToken cancellationToken, bool continueOnCapturedContext) => throw new NotImplementedException();

        public Task<PolicyResult> ExecuteAndCaptureAsync(Func<Task> action) => throw new NotImplementedException();

        public Task<PolicyResult> ExecuteAndCaptureAsync(Func<Context, Task> action, IDictionary<string, object> contextData) => throw new NotImplementedException();

        public Task<PolicyResult> ExecuteAndCaptureAsync(Func<Context, Task> action, Context context) => throw new NotImplementedException();

        public Task<PolicyResult> ExecuteAndCaptureAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<PolicyResult> ExecuteAndCaptureAsync(Func<Context, CancellationToken, Task> action, IDictionary<string, object> contextData, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<PolicyResult> ExecuteAndCaptureAsync(Func<Context, CancellationToken, Task> action, Context context, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<PolicyResult> ExecuteAndCaptureAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken, bool continueOnCapturedContext) => throw new NotImplementedException();

        public Task<PolicyResult> ExecuteAndCaptureAsync(Func<Context, CancellationToken, Task> action, IDictionary<string, object> contextData, CancellationToken cancellationToken, bool continueOnCapturedContext) => throw new NotImplementedException();

        public Task<PolicyResult> ExecuteAndCaptureAsync(Func<Context, CancellationToken, Task> action, Context context, CancellationToken cancellationToken, bool continueOnCapturedContext) => throw new NotImplementedException();

        public Task<PolicyResult<TResult1>> ExecuteAndCaptureAsync<TResult1>(Func<Task<TResult1>> action) => throw new NotImplementedException();

        public Task<PolicyResult<TResult1>> ExecuteAndCaptureAsync<TResult1>(Func<Context, Task<TResult1>> action, IDictionary<string, object> contextData) => throw new NotImplementedException();

        public Task<PolicyResult<TResult1>> ExecuteAndCaptureAsync<TResult1>(Func<Context, Task<TResult1>> action, Context context) => throw new NotImplementedException();

        public Task<PolicyResult<TResult1>> ExecuteAndCaptureAsync<TResult1>(Func<CancellationToken, Task<TResult1>> action, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<PolicyResult<TResult1>> ExecuteAndCaptureAsync<TResult1>(Func<Context, CancellationToken, Task<TResult1>> action, IDictionary<string, object> contextData, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<PolicyResult<TResult1>> ExecuteAndCaptureAsync<TResult1>(Func<Context, CancellationToken, Task<TResult1>> action, Context context, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<PolicyResult<TResult1>> ExecuteAndCaptureAsync<TResult1>(Func<CancellationToken, Task<TResult1>> action, CancellationToken cancellationToken, bool continueOnCapturedContext) => throw new NotImplementedException();

        public Task<PolicyResult<TResult1>> ExecuteAndCaptureAsync<TResult1>(Func<Context, CancellationToken, Task<TResult1>> action, IDictionary<string, object> contextData, CancellationToken cancellationToken, bool continueOnCapturedContext) => throw new NotImplementedException();

        public Task<PolicyResult<TResult1>> ExecuteAndCaptureAsync<TResult1>(Func<Context, CancellationToken, Task<TResult1>> action, Context context, CancellationToken cancellationToken, bool continueOnCapturedContext) => throw new NotImplementedException();
    }
}
