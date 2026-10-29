using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

var downstreamBuilder = WebApplication.CreateBuilder(args);

if (downstreamBuilder.Environment.IsDevelopment())
{
    downstreamBuilder.Logging.AddConsole();
}

await using var downstream = downstreamBuilder.Build();
downstream.Urls.Clear();
downstream.Urls.Add("http://localhost:5568");

downstream.MapPost("/api/files", async (HttpContext context) =>
{
    IFormCollection form = await context.Request.ReadFormAsync(context.RequestAborted);
    var file = form.Files.GetFile("file");

    if (file is null)
    {
        return Results.BadRequest(new
        {
            Error = "Expected a file field named 'file'.",
        });
    }

    string fileContents;
    using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
    {
        fileContents = await reader.ReadToEndAsync(context.RequestAborted);
    }

    return Results.Json(new
    {
        Description = form["description"].ToString(),
        File = new
        {
            FieldName = file.Name,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Contents = fileContents,
        },
        RequestContentType = context.Request.ContentType,
    });
});

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot();
builder.Services
    .AddOcelot(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

await using var gateway = builder.Build();
gateway.Urls.Clear();
gateway.Urls.Add("http://localhost:5567");
gateway.UseDefaultFiles();
gateway.UseStaticFiles();
await gateway.UseOcelot();

await downstream.StartAsync();
try
{
    await gateway.RunAsync();
}
finally
{
    await downstream.StopAsync();
}
