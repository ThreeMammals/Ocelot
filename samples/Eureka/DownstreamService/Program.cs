var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddControllers();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseHttpsRedirection()
    .UseAuthorization();
app.MapControllers();
app.Run();
