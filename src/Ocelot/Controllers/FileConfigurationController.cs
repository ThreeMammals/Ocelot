using Microsoft.AspNetCore.Mvc;
using Ocelot.Services;

namespace Ocelot.Controllers
{
    [RouteAttribute("configuration")]
    public class FileConfigurationController
    {
        private IGetFileConfiguration _getFileConfig;

        public FileConfigurationController(IGetFileConfiguration getFileConfig)
        {
            _getFileConfig = getFileConfig;
        }

        public IActionResult Get()
        {
            return new OkObjectResult(_getFileConfig.Invoke().Data);
        }
    }
}