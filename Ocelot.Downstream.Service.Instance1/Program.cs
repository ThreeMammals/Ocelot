using Ocelot.Sample.LoadBalancer.Common;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Load settings from appsettings.instance1.json
//builder.Configuration.AddJsonFile("appsettings.instance1.json", optional: true, reloadOnChange: true);

//// Load settings from appsettings.instance2.json
//builder.Configuration.AddJsonFile("appsettings.instance2.json", optional: true, reloadOnChange: true);
builder.Services.AddConsulConfig(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseConsul(builder.Configuration);

app.UseAuthorization();

app.MapControllers();

app.Run();
