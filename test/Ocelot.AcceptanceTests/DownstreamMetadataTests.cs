using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Middleware;

namespace Ocelot.AcceptanceTests;

public class DownstreamMetadataTests : IDisposable
{
    private readonly Steps _steps;
    private readonly ServiceHandler _serviceHandler;

    public DownstreamMetadataTests()
    {
        _steps = new Steps();
        _serviceHandler = new ServiceHandler();
    }

    public void Dispose()
    {
        _steps?.Dispose();
        _serviceHandler?.Dispose();
    }

    [Theory]
    [InlineData(typeof(StringDownStreamMetadataHandler))]
    [InlineData(typeof(StringArrayDownStreamMetadataHandler))]
    [InlineData(typeof(BoolDownStreamMetadataHandler))]
    [InlineData(typeof(DoubleDownStreamMetadataHandler))]
    [InlineData(typeof(SuperDataContainerDownStreamMetadataHandler))]
    public void ShouldMatchTargetObjects(Type currentType)
    {
        (Dictionary<string, string> sourceDictionary, Dictionary<string, object> _) =
            GetSourceAndTargetDictionary(currentType);

        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new List<FileHostAndPort> { new() { Host = "localhost", Port = port, }, },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    Metadata = sourceDictionary,
                    DelegatingHandlers = new List<string> { currentType.Name, },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningWithSpecificHandlerForType(currentType))
            .When(x => _steps.WhenIGetUrlOnTheApiGateway($"/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string url)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(url, context =>
        {
            context.Response.StatusCode = 200;
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Starting ocelot with the delegating handler of type currentType
    /// </summary>
    /// <param name="currentType">The current delegating handler type.</param>
    /// <exception cref="NotImplementedException">Throws if delegating handler type doesn't match.</exception>
    private void GivenOcelotIsRunningWithSpecificHandlerForType(Type currentType)
    {
        if (currentType == typeof(StringDownStreamMetadataHandler))
        {
            _steps.GivenOcelotIsRunningWithSpecificHandlerRegisteredInDi<StringDownStreamMetadataHandler>();
        }
        else if (currentType == typeof(StringArrayDownStreamMetadataHandler))
        {
            _steps.GivenOcelotIsRunningWithSpecificHandlerRegisteredInDi<StringArrayDownStreamMetadataHandler>();
        }
        else if (currentType == typeof(BoolDownStreamMetadataHandler))
        {
            _steps.GivenOcelotIsRunningWithSpecificHandlerRegisteredInDi<BoolDownStreamMetadataHandler>();
        }
        else if (currentType == typeof(DoubleDownStreamMetadataHandler))
        {
            _steps.GivenOcelotIsRunningWithSpecificHandlerRegisteredInDi<DoubleDownStreamMetadataHandler>();
        }
        else if (currentType == typeof(SuperDataContainerDownStreamMetadataHandler))
        {
            _steps.GivenOcelotIsRunningWithSpecificHandlerRegisteredInDi<SuperDataContainerDownStreamMetadataHandler>();
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    // It would have been better to use a generic method, but it is not possible to use a generic type as a parameter
    // for the delegating handler name
    private class StringDownStreamMetadataHandler : DownstreamMetadataHandler<string>
    {
        public StringDownStreamMetadataHandler(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }
    }

    private class StringArrayDownStreamMetadataHandler : DownstreamMetadataHandler<string[]>
    {
        public StringArrayDownStreamMetadataHandler(IHttpContextAccessor httpContextAccessor) : base(
            httpContextAccessor)
        {
        }
    }

    private class BoolDownStreamMetadataHandler : DownstreamMetadataHandler<bool?>
    {
        public BoolDownStreamMetadataHandler(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }
    }

    private class DoubleDownStreamMetadataHandler : DownstreamMetadataHandler<double>
    {
        public DoubleDownStreamMetadataHandler(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }
    }

    private class SuperDataContainerDownStreamMetadataHandler : DownstreamMetadataHandler<SuperDataContainer>
    {
        public SuperDataContainerDownStreamMetadataHandler(IHttpContextAccessor httpContextAccessor) : base(
            httpContextAccessor)
        {
        }
    }

    /// <summary>
    /// Simple delegating handler that checks if the metadata is correctly passed to the downstream route
    /// and checking if the extension method GetMetadata returns the correct value
    /// </summary>
    /// <typeparam name="T">The current type.</typeparam>
    private class DownstreamMetadataHandler<T> : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DownstreamMetadataHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var downstreamRoute = _httpContextAccessor.HttpContext?.Items.DownstreamRoute();

            (Dictionary<string, string> _, Dictionary<string, object> targetDictionary) =
                GetSourceAndTargetDictionary(typeof(T));

            foreach (var key in targetDictionary.Keys)
            {
                Assert.Equal(targetDictionary[key], downstreamRoute.GetMetadata<T>(key));
            }

            return base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Method retrieving the source and target dictionary for the current type.
    /// The source value is of type string and the target is of type object.
    /// </summary>
    /// <param name="currentType">The current type.</param>
    /// <returns>A source and a target directory to compare the results.</returns>
    /// <exception cref="NotImplementedException">Throws if type not found.</exception>
    public static (Dictionary<string, string> SourceDictionary, Dictionary<string, object> TargetDictionary)
        GetSourceAndTargetDictionary(Type currentType)
    {
        Dictionary<string, string> sourceDictionary;
        Dictionary<string, object> targetDictionary;
        if (currentType == typeof(StringDownStreamMetadataHandler) || currentType == typeof(string))
        {
            sourceDictionary = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" }, };

            targetDictionary = new Dictionary<string, object> { { "Key1", "Value1" }, { "Key2", "Value2" }, };

            return (sourceDictionary, targetDictionary);
        }

        if (currentType == typeof(StringArrayDownStreamMetadataHandler) || currentType == typeof(string[]))
        {
            sourceDictionary = new Dictionary<string, string>
            {
                { "Key1", "Value1, Value2, Value3" },
                { "Key2", "Value2, Value3, Value4" },
                { "Key3", "Value3, ,Value4, Value5" },
            };

            targetDictionary = new Dictionary<string, object>
            {
                { "Key1", new[] { "Value1", "Value2", "Value3" } },
                { "Key2", new[] { "Value2", "Value3", "Value4" } },
                { "Key3", new[] { "Value3", "Value4", "Value5" } },
            };

            return (sourceDictionary, targetDictionary);
        }

        if (currentType == typeof(BoolDownStreamMetadataHandler) || currentType == typeof(bool?))
        {
            sourceDictionary = new Dictionary<string, string>
            {
                { "Key1", "true" },
                { "Key2", "false" },
                { "Key3", "null" },
                { "Key4", "disabled" },
                { "Key5", "0" },
                { "Key6", "1" },
                { "Key7", "yes" },
                { "Key8", "enabled" },
                { "Key9", "on" },
                { "Key10", "off" },
                { "Key11", "test" },
            };

            targetDictionary = new Dictionary<string, object>
            {
                { "Key1", true },
                { "Key2", false },
                { "Key3", null },
                { "Key4", false },
                { "Key5", false },
                { "Key6", true },
                { "Key7", true },
                { "Key8", true },
                { "Key9", true },
                { "Key10", false },
                { "Key11", null },
            };

            return (sourceDictionary, targetDictionary);
        }

        if (currentType == typeof(DoubleDownStreamMetadataHandler) || currentType == typeof(double))
        {
            sourceDictionary = new Dictionary<string, string> { { "Key1", "0.00001" }, { "Key2", "0.00000001" }, };

            targetDictionary = new Dictionary<string, object> { { "Key1", 0.00001 }, { "Key2", 0.00000001 }, };

            return (sourceDictionary, targetDictionary);
        }

        if (currentType == typeof(SuperDataContainerDownStreamMetadataHandler) || currentType == typeof(SuperDataContainer))
        {
            sourceDictionary = new Dictionary<string, string>
            {
                { "Key1", "{\"key1\":\"Bonjour\",\"key2\":\"Hello\",\"key3\":0.00001,\"key4\":true}" },
            };

            targetDictionary = new Dictionary<string, object>
            {
                {
                    "Key1",
                    new SuperDataContainer
                    {
                        Key1 = "Bonjour",
                        Key2 = "Hello",
                        Key3 = 0.00001,
                        Key4 = true,
                    }
                },
            };

            return (sourceDictionary, targetDictionary);
        }

        throw new NotImplementedException();
    }

    public class SuperDataContainer
    {
        public string Key1 { get; set; }
        public string Key2 { get; set; }
        public double Key3 { get; set; }
        public bool? Key4 { get; set; }

        public override bool Equals(object obj)
        {
            // Check for null and compare run-time types.
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            SuperDataContainer other = (SuperDataContainer)obj;
            return Key1 == other.Key1 && Key2 == other.Key2 && Key3.Equals(other.Key3) && Key4 == other.Key4;
        }

        // https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + (Key1?.GetHashCode() ?? 0);
                hash = (hash * 23) + (Key2?.GetHashCode() ?? 0);
                hash = (hash * 23) + Key3.GetHashCode();
                hash = (hash * 23) + (Key4?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
