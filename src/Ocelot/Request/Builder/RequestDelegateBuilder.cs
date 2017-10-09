using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.Request.Builder
{
    public class RequestDelegateBuilder: IRequestBuilder
    {
        private readonly RequestDelegate _requestDelegate;

        public RequestDelegateBuilder(RequestDelegate requestDelegate)
        {
            _requestDelegate = requestDelegate;
        }

        public async Task Handle(HttpContext context)
        {
            await _requestDelegate.Invoke(context);
        }
    }
}
