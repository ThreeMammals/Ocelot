using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Provider;
using Ocelot.Configuration.Setter;

namespace Ocelot.Controllers
{
    [Authorize]
    [Route("configuration")]
    public class FileConfigurationController : Controller
    {
        private readonly IFileConfigurationProvider _configGetter;
        private readonly IFileConfigurationSetter _configSetter;

        public FileConfigurationController(IFileConfigurationProvider getFileConfig, IFileConfigurationSetter configSetter)
        {
            _configGetter = getFileConfig;
            _configSetter = configSetter;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var response = _configGetter.Get();

            if(response.IsError)
            {
                return new BadRequestObjectResult(response.Errors);
            }

            return new OkObjectResult(response.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]FileConfiguration fileConfiguration)
        {
            var response = await _configSetter.Set(fileConfiguration);
              
            if(response.IsError)
            {
                return new BadRequestObjectResult(response.Errors);
            }

            return new OkObjectResult(fileConfiguration);
        }
    }
}