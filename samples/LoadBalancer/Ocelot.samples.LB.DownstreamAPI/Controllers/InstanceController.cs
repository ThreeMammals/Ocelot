using Microsoft.AspNetCore.Mvc;

namespace Ocelot.samples.LB.DownstreamAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InstanceController : ControllerBase
    {
        [HttpGet("hostname")]
        public string GetHostName()
        {
            return Environment.MachineName + "Instance1";
        }
    }
}
