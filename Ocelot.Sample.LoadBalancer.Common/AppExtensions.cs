using Consul;
using Microsoft.AspNetCore.Builder;
<<<<<<< HEAD
=======
using Microsoft.AspNetCore.Hosting;
>>>>>>> 82a9a64d (Sample LoadBalancer application using consul)
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Sample.LoadBalancer.Common
{
    public static class AppExtensions
    {
        public static IServiceCollection AddConsulConfig(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddSingleton<IConsulClient>(sp => new ConsulClient(config =>
            {
                config.Address = new Uri("http://localhost:8500"); // Consul address
            }));
            return services;
        }

        public static IApplicationBuilder UseConsul(this IApplicationBuilder app, IConfiguration configuration)
        {
            var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
<<<<<<< HEAD
=======
            var lifetime = app.ApplicationServices.GetRequiredService<IApplicationLifetime>();
>>>>>>> 82a9a64d (Sample LoadBalancer application using consul)

            var registration = new AgentServiceRegistration()
            {
                ID = configuration["Consul:ServiceId"],
                Name = configuration["Consul:ServiceName"],
                Address = configuration["Consul:ServiceHost"], // Your API address
                Port = int.Parse(configuration["Consul:ServicePort"]), // Your API port
            };

            consulClient.Agent.ServiceDeregister(registration.ID).ConfigureAwait(true);
            consulClient.Agent.ServiceRegister(registration).ConfigureAwait(true);

            return app;
        }
    }
}
