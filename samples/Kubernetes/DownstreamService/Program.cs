using Ocelot.Samples.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

_ = DownstreamHostBuilder.Create(args);
var builder = WebApplication.CreateBuilder(args);

builder.Services
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()

    .AddHttpClient()
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.AllowInputFormatterExceptionMessages = true;

        var jOptions = options.JsonSerializerOptions;
        jOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true));
        jOptions.PropertyNameCaseInsensitive = true;
        jOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
