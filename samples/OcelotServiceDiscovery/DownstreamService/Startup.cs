using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ocelot.Samples.ServiceDiscovery.DownstreamService;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()

            .AddControllers()
            .AddJsonOptions(options =>
             {
                 options.AllowInputFormatterExceptionMessages = true;
                 var jOptions = options.JsonSerializerOptions;
                 jOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true));
                 jOptions.PropertyNameCaseInsensitive = true;
                 jOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
             });

        AddApplicationServices(services);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseHttpsRedirection();
        }

        app.UseRouting();
        app.UseEndpoints(configure =>
        {
            configure.MapControllers();
        });
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddHttpClient(); // to keep performance of HTTP Client high
        //services.AddSingleton
        //services.AddScoped
        //services.AddTransient
    }
}
