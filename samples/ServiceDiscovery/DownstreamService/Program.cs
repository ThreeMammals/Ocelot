global using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: ApiController]

var builder = WebApplication.CreateBuilder(args);

builder.Services
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()

    .AddHttpClient() // to keep performance of HTTP Client high
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.AllowInputFormatterExceptionMessages = true;
        var jOptions = options.JsonSerializerOptions;
        jOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true));
        jOptions.PropertyNameCaseInsensitive = true;
        jOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();
if (app.Environment.IsDevelopment())
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
app.MapControllers();
app.Run();
