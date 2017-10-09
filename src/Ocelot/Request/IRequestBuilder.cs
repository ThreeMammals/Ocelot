using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.Request.Builder
{
    public interface IRequestBuilder
    {
        Task Handle(HttpContext context);
    }
}
