using Microsoft.Extensions.Configuration;
using Ocelot.Configuration.File;

namespace Ocelot.Middleware;

public class BaseUrlFinder : IBaseUrlFinder
{
    private readonly IConfiguration _config;

    public BaseUrlFinder(IConfiguration config)
    {
        _config = config;
    }

    public string Find()
    {
        // Tries to get base url out of file...
        var key = $"{nameof(FileConfiguration.GlobalConfiguration)}:{nameof(FileGlobalConfiguration.BaseUrl)}";
        var baseUrl = _config.GetValue(key, string.Empty);

        // Falls back to memory config then finally default..
        return string.IsNullOrEmpty(baseUrl)
            ? _config.GetValue(nameof(FileGlobalConfiguration.BaseUrl), "http://localhost:5000")
            : baseUrl;
    }
}
