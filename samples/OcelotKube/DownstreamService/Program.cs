using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ocelot.Samples.OcelotKube.DownstreamService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
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

        AddApplicationServices(builder.Services);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapControllers();
        app.Run();
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddHttpClient(); // to keep performance of HTTP Client high
        //services.AddSingleton
        //services.AddScoped
        //services.AddTransient
    }
}
