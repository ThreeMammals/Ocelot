using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.DependencyInjection
{
    public interface IOcelotAdministrationBuilder
    {
        IServiceCollection Services { get; }
        IConfiguration ConfigurationRoot { get; }
    }
}
