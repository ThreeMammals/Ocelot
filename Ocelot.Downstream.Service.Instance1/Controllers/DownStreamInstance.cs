using Microsoft.AspNetCore.Mvc;

namespace Ocelot.Downstream.Service.Instance1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DownStreamInstance : ControllerBase
    {
        [HttpGet("hostname")]
        public string GetHostName()
        {
            return Environment.MachineName + "Instance1";
        }
    }
}
