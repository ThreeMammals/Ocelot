using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Ocelot.AcceptanceTests.Fake
{
    public class FakeService
    {
        private Task _handler;
        private IWebHost _webHostBuilder;

        public void Start(string url)
        {
            _webHostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .UseStartup<FakeStartup>()
                .Build();

            _handler = Task.Run(() => _webHostBuilder.Run());
        }

        public void Stop()
        {
            if(_webHostBuilder != null)
            {
                _webHostBuilder.Dispose();
                _handler.Wait();
            }
        }
    }
}