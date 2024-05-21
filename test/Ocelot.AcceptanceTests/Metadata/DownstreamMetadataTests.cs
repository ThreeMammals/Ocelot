using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.Metadata;
using Ocelot.Middleware;
using System.Globalization;

namespace Ocelot.AcceptanceTests.Metadata;

[Trait("Feat", "738")]
public class DownstreamMetadataTests : IDisposable
{
    private readonly Steps _steps;
    private readonly ServiceHandler _serviceHandler;

    public enum StringArrayConfig
    {
        Default = 1,
        AlternateSeparators,
        AlternateTrimChars,
        AlternateStringSplitOptions,
        Mix,
    }

    public enum NumberConfig
    {
        Default = 1,
        AlternateNumberStyle,
        AlternateCulture,
    }

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

    /// <summary>
    /// Testing the string array type with different configurations.
    /// </summary>
    /// <param name="separators">The possible separators.</param>
    /// <param name="trimChars">The trimmed characters.</param>
    /// <param name="stringSplitOption">If the empty entries should be removed.</param>
    /// <param name="currentConfig">The current test configuration.</param>
    [Theory]
    [InlineData(new[] { "," }, new[] { ' ' }, nameof(StringSplitOptions.None), StringArrayConfig.Default)]
    [InlineData(
        new[] { ";", ".", "," },
        new[] { ' ' },
        nameof(StringSplitOptions.None),
        StringArrayConfig.AlternateSeparators)]
    [InlineData(
        new[] { "," },
        new[] { ' ', ';', ':' },
        nameof(StringSplitOptions.None),
        StringArrayConfig.AlternateTrimChars)]
    [InlineData(
        new[] { "," },
        new[] { ' ' },
        nameof(StringSplitOptions.RemoveEmptyEntries),
        StringArrayConfig.AlternateStringSplitOptions)]
    [InlineData(
        new[] { ";", ".", "," },
        new[] { ' ', '_', ':' },
        nameof(StringSplitOptions.RemoveEmptyEntries),
        StringArrayConfig.Mix)]
    public void ShouldMatchTargetStringArrayAccordingToConfiguration(
        string[] separators,
        char[] trimChars,
        string stringSplitOption,
        StringArrayConfig currentConfig)
    {
        (Dictionary<string, string> sourceDictionary, Dictionary<string, string[]> _) =
            GetSourceAndTargetDictionariesForStringArrayType(currentConfig);

        sourceDictionary.Add(nameof(StringArrayConfig), currentConfig.ToString());

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
                    DelegatingHandlers = new List<string> { nameof(StringArrayDownStreamMetadataHandler) },
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                MetadataOptions = new FileMetadataOptions
                {
                    Separators = separators,
                    TrimChars = trimChars,
                    StringSplitOption = stringSplitOption,
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningWithSpecificHandlerForType(typeof(StringArrayDownStreamMetadataHandler)))
            .When(x => _steps.WhenIGetUrlOnTheApiGateway($"/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Theory]
    [InlineData(NumberStyles.Any, "de-CH", NumberConfig.Default)]
    [InlineData(NumberStyles.AllowParentheses | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign, "de-CH", NumberConfig.AlternateNumberStyle)]
    public void ShouldMatchTargetNumberAccordingToConfiguration(
        NumberStyles numberStyles,
        string cultureName,
        NumberConfig currentConfig)
    {
        (Dictionary<string, string> sourceDictionary, Dictionary<string, int> _) =
            GetSourceAndTargetDictionariesForNumberType();

        sourceDictionary.Add(nameof(NumberConfig), currentConfig.ToString());

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
                    DelegatingHandlers = new List<string> { nameof(IntDownStreamMetadataHandler) },
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                MetadataOptions = new FileMetadataOptions
                {
                    NumberStyle = numberStyles.ToString(),
                    CurrentCulture = cultureName,
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningWithSpecificHandlerForType(typeof(IntDownStreamMetadataHandler)))
            .When(x => _steps.WhenIGetUrlOnTheApiGateway($"/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string url)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(
            url,
            context =>
            {
                context.Response.StatusCode = 200;
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Starting ocelot with the delegating handler of type currentType.
    /// </summary>
    /// <param name="currentType">The current delegating handler type.</param>
    /// <exception cref="NotImplementedException">Throws if delegating handler type doesn't match.</exception>
    private void GivenOcelotIsRunningWithSpecificHandlerForType(Type currentType)
    {
        switch (currentType)
        {
            case { } t when t == typeof(StringDownStreamMetadataHandler):
                _steps.GivenOcelotIsRunningWithHandlerRegisteredInDi<StringDownStreamMetadataHandler>();
                break;
            case { } t when t == typeof(StringArrayDownStreamMetadataHandler):
                _steps.GivenOcelotIsRunningWithHandlerRegisteredInDi<StringArrayDownStreamMetadataHandler>();
                break;
            case { } t when t == typeof(BoolDownStreamMetadataHandler):
                _steps.GivenOcelotIsRunningWithHandlerRegisteredInDi<BoolDownStreamMetadataHandler>();
                break;
            case { } t when t == typeof(DoubleDownStreamMetadataHandler):
                _steps.GivenOcelotIsRunningWithHandlerRegisteredInDi<DoubleDownStreamMetadataHandler>();
                break;
            case { } t when t == typeof(SuperDataContainerDownStreamMetadataHandler):
                _steps.GivenOcelotIsRunningWithHandlerRegisteredInDi<SuperDataContainerDownStreamMetadataHandler>();
                break;
            case { } t when t == typeof(IntDownStreamMetadataHandler):
                _steps.GivenOcelotIsRunningWithHandlerRegisteredInDi<IntDownStreamMetadataHandler>();
                break;
            default:
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

    private class IntDownStreamMetadataHandler : DownstreamMetadataHandler<int>
    {
        public IntDownStreamMetadataHandler(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
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
    /// and checking if the extension method GetMetadata returns the correct value.
    /// </summary>
    /// <typeparam name="T">The current type.</typeparam>
    private class DownstreamMetadataHandler<T> : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DownstreamMetadataHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var downstreamRoute = _httpContextAccessor.HttpContext?.Items.DownstreamRoute();

            if (downstreamRoute.MetadataOptions.Metadata.ContainsKey(nameof(StringArrayConfig)))
            {
                var currentConfig =
                    Enum.Parse<StringArrayConfig>(downstreamRoute.MetadataOptions.Metadata[nameof(StringArrayConfig)]);
                downstreamRoute.MetadataOptions.Metadata.Remove(nameof(StringArrayConfig));

                (Dictionary<string, string> _, Dictionary<string, string[]> targetDictionary) =
                    GetSourceAndTargetDictionariesForStringArrayType(currentConfig);

                foreach (var key in targetDictionary.Keys)
                {
                    Assert.Equal(targetDictionary[key], downstreamRoute.GetMetadata<string[]>(key));
                }
            }
            else if (downstreamRoute.MetadataOptions.Metadata.ContainsKey(nameof(NumberConfig)))
            {
                downstreamRoute.MetadataOptions.Metadata.Remove(nameof(NumberConfig));

                (Dictionary<string, string> _, Dictionary<string, int> targetDictionary) =
                    GetSourceAndTargetDictionariesForNumberType();

                foreach (var key in targetDictionary.Keys)
                {
                    Assert.Equal(targetDictionary[key], downstreamRoute.GetMetadata<double>(key));
                }
            }
            else
            {
                (Dictionary<string, string> _, Dictionary<string, object> targetDictionary) =
                    GetSourceAndTargetDictionary(typeof(T));

                foreach (var key in targetDictionary.Keys)
                {
                    Assert.Equal(targetDictionary[key], downstreamRoute.GetMetadata<T>(key));
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }

    public static (Dictionary<string, string> SourceDictionary, Dictionary<string, string[]> TargetDictionary)
        GetSourceAndTargetDictionariesForStringArrayType(StringArrayConfig currentConfig)
    {
        Dictionary<string, string> sourceDictionary;
        Dictionary<string, string[]> targetDictionary;

        if (currentConfig == StringArrayConfig.Default)
        {
            sourceDictionary = new Dictionary<string, string>
            {
                { "Key1", "Value1, Value2, Value3" },
                { "Key2", "Value2, Value3, Value4" },
                { "Key3", "Value3, ,Value4, Value5" },
            };

            targetDictionary = new Dictionary<string, string[]>
            {
                { "Key1", new[] { "Value1", "Value2", "Value3" } },
                { "Key2", new[] { "Value2", "Value3", "Value4" } },
                { "Key3", new[] { "Value3", "Value4", "Value5" } },
            };

            return (sourceDictionary, targetDictionary);
        }

        if (currentConfig == StringArrayConfig.AlternateSeparators)
        {
            sourceDictionary = new Dictionary<string, string>
            {
                { "Key1", "Value1; Value2. Value3" },
                { "Key2", "Value2. Value3, Value4" },
                { "Key3", "Value3, ,Value4; Value5" },
            };

            targetDictionary = new Dictionary<string, string[]>
            {
                { "Key1", new[] { "Value1", "Value2", "Value3" } },
                { "Key2", new[] { "Value2", "Value3", "Value4" } },
                { "Key3", new[] { "Value3", "Value4", "Value5" } },
            };

            return (sourceDictionary, targetDictionary);
        }

        if (currentConfig == StringArrayConfig.AlternateTrimChars)
        {
            sourceDictionary = new Dictionary<string, string>
            {
                { "Key1", "Value1; :, Value2 :, Value3 " },
                { "Key2", " Value2, Value3; , Value4" },
                { "Key3", "Value3 , ,Value4, Value5 " },
            };

            targetDictionary = new Dictionary<string, string[]>
            {
                { "Key1", new[] { "Value1", "Value2", "Value3" } },
                { "Key2", new[] { "Value2", "Value3", "Value4" } },
                { "Key3", new[] { "Value3", "Value4", "Value5" } },
            };

            return (sourceDictionary, targetDictionary);
        }

        if (currentConfig == StringArrayConfig.AlternateStringSplitOptions)
        {
            sourceDictionary = new Dictionary<string, string>
            {
                { "Key1", "Value1, ,Value2, Value3, " },
                { "Key2", "Value2, , ,Value3, Value4, , ," },
                { "Key3", "Value3, ,Value4, , ,Value5" },
            };

            targetDictionary = new Dictionary<string, string[]>
            {
                { "Key1", new[] { "Value1", "Value2", "Value3" } },
                { "Key2", new[] { "Value2", "Value3", "Value4" } },
                { "Key3", new[] { "Value3", "Value4", "Value5" } },
            };

            return (sourceDictionary, targetDictionary);
        }

        if (currentConfig == StringArrayConfig.Mix)
        {
            sourceDictionary = new Dictionary<string, string>
            {
                { "Key1", "Value1; :, Value2. :, Value3 " },
                { "Key2", " Value2_, , , Value3; , Value4" },
                { "Key3", "Value3:; , ,Value4, Value5 " },
            };

            targetDictionary = new Dictionary<string, string[]>
            {
                { "Key1", new[] { "Value1", "Value2", "Value3" } },
                { "Key2", new[] { "Value2", "Value3", "Value4" } },
                { "Key3", new[] { "Value3", "Value4", "Value5" } },
            };

            return (sourceDictionary, targetDictionary);
        }

        throw new NotImplementedException();
    }

    public static (Dictionary<string, string> SourceDictionary, Dictionary<string, int> TargetDictionary)
        GetSourceAndTargetDictionariesForNumberType()
    {
        return (
            new Dictionary<string, string>
            {
                { "Key1", "-2" }, { "Key2", " (1000000) " }, { "Key3", "-1000000000  " },
            },
            new Dictionary<string, int> { { "Key1", -2 }, { "Key2", -1000000 }, { "Key3", -1000000000 } });
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

        if (currentType == typeof(SuperDataContainerDownStreamMetadataHandler) ||
            currentType == typeof(SuperDataContainer))
        {
            sourceDictionary = new Dictionary<string, string>
            {
                { "Key1", "{\"key1\":\"Bonjour\",\"key2\":\"Hello\",\"key3\":0.00001,\"key4\":true}" },
            };

            targetDictionary = new Dictionary<string, object>
            {
                {
                    "Key1", new SuperDataContainer
                    {
                        Key1 = "Bonjour", Key2 = "Hello", Key3 = 0.00001, Key4 = true,
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
