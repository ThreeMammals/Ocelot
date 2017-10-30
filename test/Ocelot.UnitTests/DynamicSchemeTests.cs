// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests
{
    public class DynamicSchemeTests
    {
        [Fact]
        public async Task OptionsAreConfiguredOnce()
        {
            var server = CreateServer(s =>
            {
                s.Configure<TestOptions>("One", o => o.Instance = new Singleton());
            });
            // Add One scheme
            var response = await server.CreateClient().GetAsync("http://example.com/add/One");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var transaction = await server.CreateClient().GetAsync("http://example.com/auth/One");
            var result = await transaction.Content.ReadAsStringAsync();
            result.ShouldBe("True");
        }

        public class TestOptions : AuthenticationSchemeOptions
        {
            public Singleton Instance { get; set; }
        }

        public class Singleton
        {
            public static int _count;

            public Singleton()
            {
                _count++;
                Count = _count;
            }

            public int Count { get; }
        }

        private class TestHandler : AuthenticationHandler<TestOptions>
        {
            public TestHandler(IOptionsMonitor<TestOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var principal = new ClaimsPrincipal();
                var id = new ClaimsIdentity("Ocelot");
                id.AddClaim(new Claim(ClaimTypes.NameIdentifier, Scheme.Name, ClaimValueTypes.String, Scheme.Name));
                if (Options.Instance != null)
                {
                    id.AddClaim(new Claim("Count", Options.Instance.Count.ToString()));
                }
                principal.AddIdentity(id);
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, new AuthenticationProperties(), Scheme.Name)));
            }
        }

        private static TestServer CreateServer(Action<IServiceCollection> configureServices = null)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use(async (context, next) =>
                    {
                        var req = context.Request;
                        var res = context.Response;
                        if (req.Path.StartsWithSegments(new PathString("/add"), out var remainder))
                        {
                            var name = remainder.Value.Substring(1);
                            var auth = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                            var scheme = new AuthenticationScheme(name, name, typeof(TestHandler));
                            auth.AddScheme(scheme);
                        }
                        else if (req.Path.StartsWithSegments(new PathString("/auth"), out remainder))
                        {
                            var name = (remainder.Value.Length > 0) ? remainder.Value.Substring(1) : null;
                            var result = await context.AuthenticateAsync(name);
                            context.User = result.Principal;
                            await res.WriteJsonAsync(context.User.Identity.IsAuthenticated.ToString());
                        }
                        else if (req.Path.StartsWithSegments(new PathString("/remove"), out remainder))
                        {
                            var name = remainder.Value.Substring(1);
                            var auth = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                            auth.RemoveScheme(name);
                        }
                        else
                        {
                            await next();
                        }
                    });
                })
                .ConfigureServices(services =>
                {
                    configureServices?.Invoke(services);
                    services.AddAuthentication();
                });
            return new TestServer(builder);
        }
    }
}