using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Ocelot.Administration;
using Ocelot.Cache;
using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;

namespace Ocelot.IntegrationTests
{
    public class AdministrationTests : IDisposable
    {
        private HttpClient _httpClient;
        private readonly HttpClient _httpClientTwo;
        private HttpResponseMessage _response;
        private IHost _builder;
        private IHostBuilder _webHostBuilder;
        private string _ocelotBaseUrl;
        private BearerToken _token;
        private IHostBuilder _webHostBuilderTwo;
        private IHost _builderTwo;
        private IHost _identityServerBuilder;
        private IHost _fooServiceBuilder;
        private IHost _barServiceBuilder;

        public AdministrationTests()
        {
            _httpClient = new HttpClient();
            _httpClientTwo = new HttpClient();
            _ocelotBaseUrl = "http://localhost:5000";
            _httpClient.BaseAddress = new Uri(_ocelotBaseUrl);
        }

        [Fact]
        public async Task Should_return_response_401_with_call_re_routes_controller()
        {
            var configuration = new FileConfiguration();
            await GivenThereIsAConfiguration(configuration);
            GivenOcelotIsRunning();
            await WhenIGetUrlOnTheApiGateway("/administration/configuration");
            ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized);
        }

        //this seems to be be answer https://github.com/IdentityServer/IdentityServer4/issues/4914
        [Fact]
        public async Task Should_return_response_200_with_call_re_routes_controller()
        {
            var configuration = new FileConfiguration();
            await GivenThereIsAConfiguration(configuration);
            GivenOcelotIsRunning();
            await GivenIHaveAnOcelotToken("/administration");
            GivenIHaveAddedATokenToMyRequest();
            await WhenIGetUrlOnTheApiGateway("/administration/configuration");
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Should_return_response_200_with_call_re_routes_controller_using_base_url_added_in_file_config()
        {
            _httpClient = new HttpClient();
            var port = PortFinder.GetRandomPort();
            _ocelotBaseUrl = $"http://localhost:{port}";
            _httpClient.BaseAddress = new Uri(_ocelotBaseUrl);

            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    BaseUrl = _ocelotBaseUrl,
                },
            };

            await GivenThereIsAConfiguration(configuration);
            await GivenOcelotIsRunningWithNoWebHostBuilder();
            await GivenIHaveAnOcelotToken("/administration");
            GivenIHaveAddedATokenToMyRequest();
            await WhenIGetUrlOnTheApiGateway("/administration/configuration");
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Should_return_OK_status_and_multiline_indented_json_response_with_json_options_for_custom_builder()
        {
            var configuration = new FileConfiguration();

            Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder = (builder, assembly) =>
            {
                return builder.AddApplicationPart(assembly)
                    .AddControllersAsServices()
                    .AddAuthorization()
                    .AddJsonOptions(options => { options.JsonSerializerOptions.WriteIndented = true; });
            };

            await GivenThereIsAConfiguration(configuration);
            GivenOcelotUsingBuilderIsRunning(customBuilder);
            await GivenIHaveAnOcelotToken("/administration");
            GivenIHaveAddedATokenToMyRequest();
            await WhenIGetUrlOnTheApiGateway("/administration/configuration");
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
            await ThenTheResultHaveMultiLineIndentedJson();
        }

        [Fact]
        public async Task Should_be_able_to_use_token_from_ocelot_a_on_ocelot_b()
        {
            var configuration = new FileConfiguration();
            var port = PortFinder.GetRandomPort();
            await GivenThereIsAConfiguration(configuration);
            GivenIdentityServerSigningEnvironmentalVariablesAreSet();
            GivenOcelotIsRunning();
            await GivenIHaveAnOcelotToken("/administration");
            await GivenAnotherOcelotIsRunning($"http://localhost:{port}");
            await WhenIGetUrlOnTheSecondOcelot("/administration/configuration");
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Should_return_file_configuration()
        {
            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    RequestIdKey = "RequestId",
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "https",
                        Host = "127.0.0.1",
                    },
                },
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = 80,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/",
                        FileCacheOptions = new FileCacheOptions
                        {
                            TtlSeconds = 10,
                            Region = "Geoff",
                        },
                    },
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = 80,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/test",
                        FileCacheOptions = new FileCacheOptions
                        {
                            TtlSeconds = 10,
                            Region = "Dave",
                        },
                    },
                },
            };

            await GivenThereIsAConfiguration(configuration);
            GivenOcelotIsRunning();
            await GivenIHaveAnOcelotToken("/administration");
            GivenIHaveAddedATokenToMyRequest();
            await WhenIGetUrlOnTheApiGateway("/administration/configuration");
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
            await ThenTheResponseShouldBe(configuration);
        }

        [Fact]
        public async Task Should_get_file_configuration_edit_and_post_updated_version()
        {
            var initialConfiguration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration(),
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = 80,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/",
                    },
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = 80,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/test",
                    },
                },
            };

            var updatedConfiguration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration(),
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = 80,
                            },
                        },
                        DownstreamScheme = "http",
                        DownstreamPathTemplate = "/geoffrey",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/",
                    },
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "123.123.123",
                                Port = 443,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/blooper/{productId}",
                        UpstreamHttpMethod = new List<string> { "post" },
                        UpstreamPathTemplate = "/test/{productId}",
                    },
                },
            };

            await GivenThereIsAConfiguration(initialConfiguration);
            GivenOcelotIsRunning();
            await GivenIHaveAnOcelotToken("/administration");
            GivenIHaveAddedATokenToMyRequest();
            await WhenIGetUrlOnTheApiGateway("/administration/configuration");
            await WhenIPostOnTheApiGateway("/administration/configuration", updatedConfiguration);
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
            await ThenTheResponseShouldBe(updatedConfiguration);
            await WhenIGetUrlOnTheApiGateway("/administration/configuration");
            await ThenTheResponseShouldBe(updatedConfiguration);
            ThenTheConfigurationIsSavedCorrectly(updatedConfiguration);
        }

        [Fact]
        public async Task Should_activate_change_token_when_configuration_is_updated()
        {
            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration(),
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = 80,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/",
                    },
                },
            };

            await GivenThereIsAConfiguration(configuration);
            GivenOcelotIsRunning();
            await GivenIHaveAnOcelotToken("/administration");
            GivenIHaveAddedATokenToMyRequest();
            await WhenIPostOnTheApiGateway("/administration/configuration", configuration);
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
            TheChangeTokenShouldBeActive();
            await ThenTheResponseShouldBe(configuration);
            await WhenIGetUrlOnTheApiGateway("/administration/configuration");
            await ThenTheResponseShouldBe(configuration);
            ThenTheConfigurationIsSavedCorrectly(configuration);
        }

        private void TheChangeTokenShouldBeActive()
        {
            _builder.Services.GetRequiredService<IOcelotConfigurationChangeTokenSource>().ChangeToken.HasChanged.ShouldBeTrue();
        }

        private static void ThenTheConfigurationIsSavedCorrectly(FileConfiguration expected)
        {
            var ocelotJsonPath = $"{AppContext.BaseDirectory}ocelot.json";
            var resultText = File.ReadAllText(ocelotJsonPath);
            var expectedText = JsonConvert.SerializeObject(expected, Formatting.Indented);
            resultText.ShouldBe(expectedText);

            var environmentSpecificPath = $"{AppContext.BaseDirectory}/ocelot.Production.json";
            resultText = File.ReadAllText(environmentSpecificPath);
            expectedText = JsonConvert.SerializeObject(expected, Formatting.Indented);
            resultText.ShouldBe(expectedText);
        }

        [Fact]
        public async Task Should_get_file_configuration_edit_and_post_updated_version_redirecting_route()
        {
            var fooPort = PortFinder.GetRandomPort();
            var barPort = PortFinder.GetRandomPort();

            var initialConfiguration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = fooPort,
                            },
                        },
                        DownstreamScheme = "http",
                        DownstreamPathTemplate = "/foo",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/foo",
                    },
                },
            };

            var updatedConfiguration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration(),
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = barPort,
                            },
                        },
                        DownstreamScheme = "http",
                        DownstreamPathTemplate = "/bar",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/foo",
                    },
                },
            };

            await GivenThereIsAConfiguration(initialConfiguration);
            GivenThereIsAFooServiceRunningOn($"http://localhost:{fooPort}");
            GivenThereIsABarServiceRunningOn($"http://localhost:{barPort}");
            GivenOcelotIsRunning();
            await WhenIGetUrlOnTheApiGateway("/foo");
            await ThenTheResponseBodyShouldBe("foo");
            await GivenIHaveAnOcelotToken("/administration");
            GivenIHaveAddedATokenToMyRequest();
            await WhenIPostOnTheApiGateway("/administration/configuration", updatedConfiguration);
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
            await ThenTheResponseShouldBe(updatedConfiguration);
            await WhenIGetUrlOnTheApiGateway("/foo");
            await ThenTheResponseBodyShouldBe("bar");
            await WhenIPostOnTheApiGateway("/administration/configuration", initialConfiguration);
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
            await ThenTheResponseShouldBe(initialConfiguration);
            await WhenIGetUrlOnTheApiGateway("/foo");
            await ThenTheResponseBodyShouldBe("foo");
        }

        [Fact]
        public async Task Should_clear_region()
        {
            var initialConfiguration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration(),
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = 80,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/",
                        FileCacheOptions = new FileCacheOptions
                        {
                            TtlSeconds = 10,
                        },
                    },
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = 80,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/test",
                        FileCacheOptions = new FileCacheOptions
                        {
                            TtlSeconds = 10,
                        },
                    },
                },
            };

            var regionToClear = "gettest";
            await GivenThereIsAConfiguration(initialConfiguration);
            GivenOcelotIsRunning();
            await GivenIHaveAnOcelotToken("/administration");
            GivenIHaveAddedATokenToMyRequest();
            await WhenIDeleteOnTheApiGateway($"/administration/outputcache/{regionToClear}");
            ThenTheStatusCodeShouldBe(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Should_return_response_200_with_call_re_routes_controller_when_using_own_identity_server_to_secure_admin_area()
        {
            var configuration = new FileConfiguration();

            var port = PortFinder.GetRandomPort();
            var identityServerRootUrl = $"http://localhost:{port}";

            Action<JwtBearerOptions> options = o =>
            {
                o.Authority = identityServerRootUrl;
                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                };
            };

            await GivenThereIsAConfiguration(configuration);
            await GivenThereIsAnIdentityServerOn(identityServerRootUrl, "api");
            await GivenOcelotIsRunningWithIdentityServerSettings(options);
            await GivenIHaveAToken(identityServerRootUrl);
            GivenIHaveAddedATokenToMyRequest();
            await WhenIGetUrlOnTheApiGateway("/administration/configuration");
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        }

        private async Task GivenIHaveAToken(string url)
        {
            var formData = new List<KeyValuePair<string, string>>
            {
                new("client_id", "api"),
                new("client_secret", "secret"),
                new("scope", "api"),
                new("username", "test"),
                new("password", "test"),
                new("grant_type", "password"),
            };
            var content = new FormUrlEncodedContent(formData);

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync($"{url}/connect/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
        }

        private async Task GivenThereIsAnIdentityServerOn(string url, string apiName)
        {
            _identityServerBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseUrls(url)
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureServices(services =>
                    {
                        services.AddLogging();
                        services.AddIdentityServer()
                        .AddDeveloperSigningCredential()
                        .AddInMemoryApiScopes(new List<ApiScope> { new(apiName) })
                        .AddInMemoryApiResources(new List<ApiResource>
                        {
                        new()
                        {
                            Name = apiName,
                            Description = apiName,
                            Enabled = true,
                            DisplayName = apiName,
                            Scopes = new List<string>
                            {
                                apiName,
                            },
                        },
                        })
                        .AddInMemoryClients(new List<Client>
                        {
                        new()
                        {
                            ClientId = apiName,
                            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                            ClientSecrets = new List<Secret> { new("secret".Sha256()) },
                            AllowedScopes = new List<string> { apiName },
                            AccessTokenType = AccessTokenType.Jwt,
                            Enabled = true,
                        },
                        })
                        .AddTestUsers(new List<TestUser>
                        {
                        new()
                        {
                            Username = "test",
                            Password = "test",
                            SubjectId = "1231231",
                        },
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseIdentityServer();
                    }
                    );
                }).Build();

            await _identityServerBuilder.StartAsync();

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{url}/.well-known/openid-configuration");
            response.EnsureSuccessStatusCode();
        }

        private async Task GivenAnotherOcelotIsRunning(string baseUrl)
        {
            _httpClientTwo.BaseAddress = new Uri(baseUrl);

            _webHostBuilderTwo = Host.CreateDefaultBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseUrls(baseUrl)
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                        var env = hostingContext.HostingEnvironment;
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                        config.AddJsonFile("ocelot.json", false, false);
                        config.AddEnvironmentVariables();
                    })
                    .ConfigureServices(x =>
                    {
                        x.AddMvc(option => option.EnableEndpointRouting = false);
                        x.AddOcelot()
                       .AddAdministration("/administration", "secret");
                    })
                    .Configure(async app =>
                    {
                        await app.UseOcelot();
                    });
                });

            _builderTwo = _webHostBuilderTwo.Build();

            await _builderTwo.StartAsync();
        }

        private static void GivenIdentityServerSigningEnvironmentalVariablesAreSet()
        {
            Environment.SetEnvironmentVariable("OCELOT_CERTIFICATE", "mycert.pfx");
            Environment.SetEnvironmentVariable("OCELOT_CERTIFICATE_PASSWORD", "password");
        }

        private async Task WhenIGetUrlOnTheSecondOcelot(string url)
        {
            _httpClientTwo.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
            _response = await _httpClientTwo.GetAsync(url);
        }

        private async Task WhenIPostOnTheApiGateway(string url, FileConfiguration updatedConfiguration)
        {
            var json = JsonConvert.SerializeObject(updatedConfiguration);
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            _response = await _httpClient.PostAsync(url, content);
        }

        private async Task ThenTheResponseBodyShouldBe(string expected)
        {
            var content = await _response.Content.ReadAsStringAsync();
            content.ShouldBe(expected);
        }

        private async Task ThenTheResponseShouldBe(FileConfiguration expecteds)
        {
            var response = JsonConvert.DeserializeObject<FileConfiguration>(await _response.Content.ReadAsStringAsync());

            response.GlobalConfiguration.RequestIdKey.ShouldBe(expecteds.GlobalConfiguration.RequestIdKey);
            response.GlobalConfiguration.ServiceDiscoveryProvider.Scheme.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Scheme);
            response.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            response.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Port);

            for (var i = 0; i < response.Routes.Count; i++)
            {
                for (var j = 0; j < response.Routes[i].DownstreamHostAndPorts.Count; j++)
                {
                    var result = response.Routes[i].DownstreamHostAndPorts[j];
                    var expected = expecteds.Routes[i].DownstreamHostAndPorts[j];
                    result.Host.ShouldBe(expected.Host);
                    result.Port.ShouldBe(expected.Port);
                }

                response.Routes[i].DownstreamPathTemplate.ShouldBe(expecteds.Routes[i].DownstreamPathTemplate);
                response.Routes[i].DownstreamScheme.ShouldBe(expecteds.Routes[i].DownstreamScheme);
                response.Routes[i].UpstreamPathTemplate.ShouldBe(expecteds.Routes[i].UpstreamPathTemplate);
                response.Routes[i].UpstreamHttpMethod.ShouldBe(expecteds.Routes[i].UpstreamHttpMethod);
            }
        }

        private void GivenIHaveAddedATokenToMyRequest()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
        }

        private async Task GivenIHaveAnOcelotToken(string adminPath)
        {
            var tokenUrl = $"{adminPath}/connect/token";
            var formData = new List<KeyValuePair<string, string>>
            {
                new("client_id", "admin"),
                new("client_secret", "secret"),
                new("scope", "admin"),
                new("grant_type", "client_credentials"),
            };
            var content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.PostAsync(tokenUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
            var configPath = $"{adminPath}/.well-known/openid-configuration";
            response = await _httpClient.GetAsync(configPath);
            response.EnsureSuccessStatusCode();
        }

        private async Task GivenOcelotIsRunningWithIdentityServerSettings(Action<JwtBearerOptions> configOptions)
        {
            _webHostBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseUrls(_ocelotBaseUrl)
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                        var env = hostingContext.HostingEnvironment;
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                        config.AddJsonFile("ocelot.json", false, false);
                        config.AddEnvironmentVariables();
                    })
                    .ConfigureServices(x =>
                    {
                        x.AddMvc(option => option.EnableEndpointRouting = false);
                        x.AddSingleton(_webHostBuilder);
                        x.AddOcelot()
                        .AddAdministration("/administration", configOptions);
                    })
                    .Configure(async app =>
                    {
                        await app.UseOcelot();
                    });
                });

            _builder = _webHostBuilder.Build();

            await _builder.StartAsync();
        }

        private void OcelotIsRunningWithServices(Action<IServiceCollection> configureServices)
        {
            _webHostBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseUrls(_ocelotBaseUrl)
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                        var env = hostingContext.HostingEnvironment;
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                        config.AddJsonFile("ocelot.json", false, false);
                        config.AddEnvironmentVariables();
                    })
                    .ConfigureServices(configureServices) // !!!
                    .Configure(async app =>
                    {
                        await app.UseOcelot();
                    });
                });
            _builder = _webHostBuilder.Build();
            _builder.Start();
        }

        private void GivenOcelotIsRunning()
        {
            OcelotIsRunningWithServices(services =>
            {
                services.AddMvc(s => s.EnableEndpointRouting = false);
                services.AddOcelot()
                    .AddAdministration("/administration", "secret");
            });
        }

        private void GivenOcelotUsingBuilderIsRunning(Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)
        {
            OcelotIsRunningWithServices(services =>
            {
                services.AddMvc(s => s.EnableEndpointRouting = false);
                services.AddOcelotUsingBuilder(customBuilder)
                    .AddAdministration("/administration", "secret");
            });
        }

        private async Task GivenOcelotIsRunningWithNoWebHostBuilder()
        {
            _webHostBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseUrls(_ocelotBaseUrl)
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                        var env = hostingContext.HostingEnvironment;
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                        config.AddJsonFile("ocelot.json", false, false);
                        config.AddEnvironmentVariables();
                    })
                    .ConfigureServices(x =>
                    {
                        x.AddMvc(option => option.EnableEndpointRouting = false);
                        x.AddSingleton(_webHostBuilder);
                        x.AddOcelot()
                        .AddAdministration("/administration", "secret");
                    })
                    .Configure(async app =>
                    {
                        await app.UseOcelot();
                    });
                });

            _builder = _webHostBuilder.Build();

            await _builder.StartAsync();
        }

        private static async Task GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
        {
            var configurationPath = $"{Directory.GetCurrentDirectory()}/ocelot.json";

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            await File.WriteAllTextAsync(configurationPath, jsonConfiguration);

            _ = await File.ReadAllTextAsync(configurationPath);

            configurationPath = $"{AppContext.BaseDirectory}/ocelot.json";

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            await File.WriteAllTextAsync(configurationPath, jsonConfiguration);

            _ = await File.ReadAllTextAsync(configurationPath);
        }

        private async Task WhenIGetUrlOnTheApiGateway(string url)
        {
            _response = await _httpClient.GetAsync(url);
        }

        private async Task WhenIDeleteOnTheApiGateway(string url)
        {
            _response = await _httpClient.DeleteAsync(url);
        }

        private void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }

        private async Task ThenTheResultHaveMultiLineIndentedJson()
        {
            const string indent = "  ";
            const int total = 52, skip = 1;
            var contentAsString = await _response.Content.ReadAsStringAsync();
            string[] lines = contentAsString.Split(Environment.NewLine);
            lines.Length.ShouldBeGreaterThanOrEqualTo(total);
            lines.First().ShouldNotStartWith(indent);

            lines.Skip(skip).Take(total - skip - 1).ToList()
                .ForEach(line => line.ShouldStartWith(indent));

            lines.Last().ShouldNotStartWith(indent);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("OCELOT_CERTIFICATE", string.Empty);
            Environment.SetEnvironmentVariable("OCELOT_CERTIFICATE_PASSWORD", string.Empty);
            _builder?.Dispose();
            _httpClient?.Dispose();
            _identityServerBuilder?.Dispose();
            GC.SuppressFinalize(this);
        }

        private void GivenThereIsAFooServiceRunningOn(string baseUrl)
        {
            _fooServiceBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseUrls(baseUrl)
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        app.UsePathBase("/foo");
                        app.Run(async context =>
                        {
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync("foo");
                        });
                    });
                }).Build();

            _fooServiceBuilder.Start();
        }

        private void GivenThereIsABarServiceRunningOn(string baseUrl)
        {
            _barServiceBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseUrls(baseUrl)
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        app.UsePathBase("/bar");
                        app.Run(async context =>
                        {
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync("bar");
                        });
                    });
                }).Build();

            _barServiceBuilder.Start();
        }
    }
}
