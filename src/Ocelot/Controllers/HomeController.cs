using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ocelot.Controllers
{
    /// <summary>
    /// This controller is a catch all for requests so that if we build it into a pipeline
    /// the requests get authorised.
    /// </summary>
    public class HomeController : Controller
    {
        //[Authorize]
        [Route("{*url}")]
        public void Index()
        {
            if (true == true)
            {
                
            }
        }
    }
}
