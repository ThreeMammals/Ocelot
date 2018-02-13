using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class HttpHandlerOptionsCreator : IHttpHandlerOptionsCreator
    {
        public HttpHandlerOptions Create(FileReRoute fileReRoute)
        {
            return new HttpHandlerOptions(fileReRoute.HttpHandlerOptions.AllowAutoRedirect,
                fileReRoute.HttpHandlerOptions.UseCookieContainer, fileReRoute.HttpHandlerOptions.UseTracing);
        }
    }
}
