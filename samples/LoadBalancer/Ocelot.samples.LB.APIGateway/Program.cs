using Ocelot.DependencyInjection;
using Ocelot.Provider.Consul;
using Ocelot.Middleware;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Configuration.AddJsonFile("Ocelot.json");
builder.Services.AddOcelot(builder.Configuration).AddConsul();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseOcelot().Wait();
app.MapControllers();
app.Run();

