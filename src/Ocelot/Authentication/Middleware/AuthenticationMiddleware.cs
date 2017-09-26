using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Authentication.Handler.Factory;
using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Authentication.Middleware
{
    public class AuthenticationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IApplicationBuilder _app;
        private readonly IAuthenticationHandlerFactory _authHandlerFactory;
        private readonly IOcelotLogger _logger;

        public AuthenticationMiddleware(RequestDelegate next,
            IApplicationBuilder app,
            IRequestScopedDataRepository requestScopedDataRepository,
            IAuthenticationHandlerFactory authHandlerFactory,
            IOcelotLoggerFactory loggerFactory)
            : base(requestScopedDataRepository)
        {
            _next = next;
            _authHandlerFactory = authHandlerFactory;
            _app = app;
            _logger = loggerFactory.CreateLogger<AuthenticationMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
         /*   var req = context.Request;
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
                result.Principal.IsAuthenticated();
            }
            else if (req.Path.StartsWithSegments(new PathString("/remove"), out remainder))
            {
                var name = remainder.Value.Substring(1);
                var auth = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                auth.RemoveScheme(name);
            }
            else
            {
                await _next.Invoke(context);
            }*/

            if (IsAuthenticatedRoute(DownstreamRoute.ReRoute))
            {
                _logger.LogDebug($"{context.Request.Path} is an authenticated route. {MiddlewareName} checking if client is authenticated");

                var authenticationHandler = _authHandlerFactory.Get(_app, DownstreamRoute.ReRoute.AuthenticationOptions);

                if (authenticationHandler.IsError)
                {
                    _logger.LogError($"Error getting authentication handler for {context.Request.Path}. {authenticationHandler.Errors.ToErrorString()}");
                    SetPipelineError(authenticationHandler.Errors);
                    return;
                }

                await authenticationHandler.Data.Handler.Handle(context);


                if (context.User.Identity.IsAuthenticated)
                {
                    _logger.LogDebug($"Client has been authenticated for {context.Request.Path}");
                    await _next.Invoke(context);
                }
                else
                {
                    var error = new List<Error>
                    {
                        new UnauthenticatedError(
                            $"Request for authenticated route {context.Request.Path} by {context.User.Identity.Name} was unauthenticated")
                    };

                    _logger.LogError($"Client has NOT been authenticated for {context.Request.Path} and pipeline error set. {error.ToErrorString()}");
                    SetPipelineError(error);
                    return;
                }
            }
            else
            {
                _logger.LogTrace($"No authentication needed for {context.Request.Path}");

                await _next.Invoke(context);
            }
        }

        private static bool IsAuthenticatedRoute(ReRoute reRoute)
        {
            return reRoute.IsAuthenticated;
        }
    }

    public class TestHandler : AuthenticationHandler<TestOptions>
    {
        public TestHandler(IOptionsMonitor<TestOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var principal = new ClaimsPrincipal();
            var id = new ClaimsIdentity();
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, Scheme.Name, ClaimValueTypes.String, Scheme.Name));
            if (Options.Instance != null)
            {
                id.AddClaim(new Claim("Count", Options.Instance.Count.ToString()));
            }
            principal.AddIdentity(id);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, new AuthenticationProperties(), Scheme.Name)));
        }
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
}

