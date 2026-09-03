using System.Text;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();

app.MapPost("/api/files", async (HttpContext context) =>
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

app.Run();
