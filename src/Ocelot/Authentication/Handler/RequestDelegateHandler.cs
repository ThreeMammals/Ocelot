using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Ocelot.Authentication.Handler
{
    public class RequestDelegateHandler : IHandler
    {
        private readonly RequestDelegate _requestDelegate;

        public RequestDelegateHandler(RequestDelegate requestDelegate)
        {
            _requestDelegate = requestDelegate;
        }

        public async Task Handle(HttpContext context)
        {
            await _requestDelegate.Invoke(context);
        }
    }
}