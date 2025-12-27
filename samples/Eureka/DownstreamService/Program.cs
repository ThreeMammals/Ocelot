using Steeltoe.Discovery.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddDiscoveryClient(builder.Configuration)
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
