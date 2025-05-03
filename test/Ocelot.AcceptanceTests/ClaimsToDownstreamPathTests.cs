﻿//using IdentityServer4.AccessTokenValidation;
//using IdentityServer4.Models;
//using IdentityServer4.Test;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.AcceptanceTests.Authentication;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public sealed class ClaimsToDownstreamPathTests : AuthenticationSteps
{
    private IWebHost _servicebuilder;

    //private readonly IWebHost _identityServerBuilder;
    //private readonly Action<IdentityServerAuthenticationOptions> _options;
    private readonly string _identityServerRootUrl;
    private string _downstreamFinalPath;

    public ClaimsToDownstreamPathTests()
    {
        var identityServerPort = PortFinder.GetRandomPort();
        _identityServerRootUrl = $"http://localhost:{identityServerPort}";

        //_options = o =>
        //{
        //    o.Authority = _identityServerRootUrl;
        //    o.ApiName = "api";
        //    o.RequireHttpsMetadata = false;
        //    o.SupportedTokens = SupportedTokens.Both;
        //    o.ApiSecret = "secret";
        //};
    }

    [Fact(Skip = "TODO: Requires redevelopment because IdentityServer4 is deprecated")]
    public void Should_return_200_and_change_downstream_path()
    {
        //var user = new TestUser
        //{
        //    Username = "test",
        //    Password = "test",
        //    SubjectId = "registered|1231231",
        //};
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
               {
                   new()
                   {
                       DownstreamPathTemplate = "/users/{userId}",
                       DownstreamHostAndPorts = new List<FileHostAndPort>
                       {
                           new()
                           {
                               Host = "localhost",
                               Port = port,
                           },
                       },
                       DownstreamScheme = "http",
                       UpstreamPathTemplate = "/users/{userId}",
                       UpstreamHttpMethod = new List<string> { "Get" },
                       AuthenticationOptions = new FileAuthenticationOptions
                       {
                           AuthenticationProviderKey = "Test",
                           AllowedScopes = new List<string>
                           {
                               "openid", "offline_access", "api",
                           },
                       },
                       ChangeDownstreamPathTemplate =
                       {
                           {"userId", "Claims[sub] > value[1] > |"},
                       },
                   },
               },
        };

        this.Given(x => null) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt, user))
            .And(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), 200))
            .And(x => GivenIHaveAToken(_identityServerRootUrl))
            .And(x => GivenThereIsAConfiguration(configuration))

            //.And(x => GivenOcelotIsRunning(_options, "Test"))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/users"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("UserId: 1231231"))
            .And(x => ThenTheDownstreamPathIs("/users/1231231"))
            .BDDfy();
    }

    private void ThenTheDownstreamPathIs(string path)
    {
        _downstreamFinalPath.ShouldBe(path);
    }

    private void GivenThereIsAServiceRunningOn(string url, int statusCode)
    {
        _servicebuilder = TestHostBuilder.Create()
            .UseUrls(url)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseUrls(url)
            .Configure(app =>
            {
                app.Run(async context =>
                {
                    _downstreamFinalPath = context.Request.Path.Value;

                    var userId = _downstreamFinalPath.Replace("/users/", string.Empty);

                    var responseBody = $"UserId: {userId}";
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync(responseBody);
                });
            })
            .Build();

        _servicebuilder.Start();
    }

    //private async Task GivenThereIsAnIdentityServerOn(string url, string apiName, AccessTokenType tokenType, TestUser user)
    //{
    //    _identityServerBuilder = TestHostBuilder.Create()
    //        .UseUrls(url)
    //        .UseKestrel()
    //        .UseContentRoot(Directory.GetCurrentDirectory())
    //        .UseIISIntegration()
    //        .UseUrls(url)
    //        .ConfigureServices(services =>
    //        {
    //            services.AddLogging();
    //            services.AddIdentityServer()
    //                .AddDeveloperSigningCredential()
    //                .AddInMemoryApiScopes(new List<ApiScope>
    //                {
    //                    new(apiName, "test"),
    //                    new("openid", "test"),
    //                    new("offline_access", "test"),
    //                    new("api.readOnly", "test"),
    //                })
    //                .AddInMemoryApiResources(new List<ApiResource>
    //                {
    //                    new()
    //                    {
    //                        Name = apiName,
    //                        Description = "My API",
    //                        Enabled = true,
    //                        DisplayName = "test",
    //                        Scopes = new List<string>
    //                        {
    //                            "api",
    //                            "openid",
    //                            "offline_access",
    //                        },
    //                        ApiSecrets = new List<Secret>
    //                        {
    //                            new()
    //                            {
    //                                Value = "secret".Sha256(),
    //                            },
    //                        },
    //                        UserClaims = new List<string>
    //                        {
    //                            "CustomerId", "LocationId", "UserType", "UserId",
    //                        },
    //                    },
    //                })
    //                .AddInMemoryClients(new List<Client>
    //                {
    //                    new()
    //                    {
    //                        ClientId = "client",
    //                        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
    //                        ClientSecrets = new List<Secret> {new("secret".Sha256())},
    //                        AllowedScopes = new List<string> { apiName, "openid", "offline_access" },
    //                        AccessTokenType = tokenType,
    //                        Enabled = true,
    //                        RequireClientSecret = false,
    //                    },
    //                })
    //                .AddTestUsers(new List<TestUser>
    //                {
    //                    user,
    //                });
    //        })
    //        .Configure(app =>
    //        {
    //            app.UseIdentityServer();
    //        })
    //        .Build();

    //    await _identityServerBuilder.StartAsync();
    //    await Steps.VerifyIdentityServerStarted(url);
    //}
    public override void Dispose()
    {
        //_identityServerBuilder?.Dispose();
        _servicebuilder?.Dispose();
        base.Dispose();
    }
}
