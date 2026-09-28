using Microsoft.AspNetCore.Authentication.Negotiate;
using System.Security.Principal;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
builder.Services
    .AddAuthorization(options => options.FallbackPolicy = options.DefaultPolicy);

builder.WebHost.UseIISIntegration();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.Run(MapWindowsAuthentication);
app.Run();

static Task MapWindowsAuthentication(HttpContext context)
{
  var response = context.Response;
  var identity = context.User.Identity;
  // var currentUser = WindowsIdentity.GetCurrent();
  if (identity is not null && identity.IsAuthenticated)
    // && identity is WindowsIdentity && identity.Name == currentUser.Name)
  {
    response.StatusCode = StatusCodes.Status200OK;
    return response.WriteAsync("Windows Authentication succeeded", context.RequestAborted);
  }

  response.StatusCode = StatusCodes.Status401Unauthorized;
  return Task.CompletedTask;
}
