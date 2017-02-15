using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ocelot.Controllers
{
    [RouteAttribute("reroutes")]
    public class ReRoutesController
    {
        public IActionResult Get()
        {
            return new OkObjectResult("hi from re routes controller");
        }
    }
}