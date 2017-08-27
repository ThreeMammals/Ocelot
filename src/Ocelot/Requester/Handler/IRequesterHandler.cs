using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.Requester.Handler
{
    public interface IRequesterHandler
    {
        Task Handle(HttpContext context);
    }
}
