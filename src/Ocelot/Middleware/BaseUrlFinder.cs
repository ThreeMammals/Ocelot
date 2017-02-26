using Microsoft.AspNetCore.Hosting;

namespace Ocelot.Middleware
{
    public class BaseUrlFinder : IBaseUrlFinder
    {
        private readonly IWebHostBuilder _webHostBuilder;

        public BaseUrlFinder(IWebHostBuilder webHostBuilder)
        {
            _webHostBuilder = webHostBuilder;
        }

        public string Find()
        {
            var baseSchemeUrlAndPort = _webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);

            return string.IsNullOrEmpty(baseSchemeUrlAndPort) ? "http://localhost:5000" : baseSchemeUrlAndPort;
        }
    }
}
