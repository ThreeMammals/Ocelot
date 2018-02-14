using Microsoft.Extensions.Configuration;

namespace Ocelot.Middleware
{
    public class BaseUrlFinder : IBaseUrlFinder
    {
        private readonly IConfiguration _config;

        public BaseUrlFinder(IConfiguration config)
        {
            _config = config;
        }

        public string Find()
        {
            var baseUrl = _config.GetValue("BaseUrl", "");

            return string.IsNullOrEmpty(baseUrl) ? "http://localhost:5000" : baseUrl;
        }
    }
}
