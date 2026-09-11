#pragma warning disable IDE0059 // Unnecessary assignment of a value

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Samples;
using Ocelot.Samples.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Ocelot Configuration setup to merge config files in the "ocelot-configuration" folder
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot("ocelot-configuration", builder.Environment); // multiple environment files (ocelot.*.json) to be merged to ocelot.json file and write it back to disk
    //.AddOcelot("ocelot-configuration", builder.Environment, MergeOcelotJson.ToMemory); // to be merged to ocelot.json JSON-data and keep it in memory

var configuration = builder.Configuration;
var ocRoot = configuration.OcelotRoot();
var ocRoutes = configuration.OcelotRoutes();
var ocGlobal = configuration.OcelotGlobalConfiguration();
var ocBaseUrl = configuration.OcelotGlobalSection(nameof(FileGlobalConfiguration.BaseUrl));
var baseUrl = configuration.OcelotGlobalProperty<string>(nameof(FileGlobalConfiguration.BaseUrl));

#region WORKING WITH GLOBAL OPTIONS of the GlobalConfiguration section via IConfiguration interface

// Adds prefix to lookup path or section name to search inside of the Ocelot's GlobalConfiguration section
static string GlobalPath(string name) => $"{nameof(FileConfiguration.GlobalConfiguration)}:{name}";

string defCountry = configuration[GlobalPath("DefaultCountry")]; // custom property
string defaultCountry = configuration.OcelotGlobalSection("DefaultCountry").Get<string>(); // custom property
defaultCountry = configuration.OcelotGlobalProperty<string>("DefaultCountry"); // custom property

string defCities = configuration[GlobalPath("DefaultCities")]; // custom array
string[] defaultCities = configuration.OcelotGlobalSection("DefaultCities").Get<string[]>(); // custom array
defaultCities = configuration.OcelotGlobalProperty<string[]>("DefaultCities"); // custom array

string defInfo = configuration[GlobalPath("Defaults")]; // custom options
DefaultInfo defaults = configuration.OcelotGlobalSection("Defaults").Get<DefaultInfo>(); // custom property
defaults = configuration.OcelotGlobalProperty<DefaultInfo>("Defaults"); // custom property
#endregion

#region WORKING WITH ROUTES of the Routes section via IConfiguration interface

var routeWeather = configuration.OcelotRoute("/weather/current/{city}"); // get by UpstreamPathTemplate, case sensitive
var weatherDefaultCity = routeWeather["DefaultCity"];
weatherDefaultCity = configuration.OcelotProperty<string>("DefaultCity", routeWeather);
var weatherDefaultCountry = configuration.OcelotProperty<string>("DefaultCountry", routeWeather);

var routePosts = configuration.OcelotRoute(key: "R3"); // get by Key, case sensitive, thus 'R3' and 'r3' are different routes
var postsDefaultCountry = configuration.OcelotProperty<string>("DefaultCountry", routePosts); // the route has no custom properties, thus, we read global property
#endregion

baseUrl = defaultCountry = weatherDefaultCity = weatherDefaultCountry = postsDefaultCountry = null;
defaultCities = null;
defaults = null;

#region WORKING WITH CONFIGURATION using Newtonsoft's OcelotJ* helpers via IConfigurationBuilder interface
// This is the JSON representation of the merged ocelot.json file or stream.
// Feel free to process it as you see fit — you can parse it with any library, search for route data, custom properties, and options using any tool of your choice.
string otJson = configuration.OcelotJson(); // this is JSON of merged ocelot.json file

// OcelotJ* helpers utilizing Newtonsoft's LINQ lib
JObject jRoot = configuration.OcelotJObject(); // this is parsed JSON of merged ocelot.json file aka JObject.Parse(otJson)
JArray jRoutes = configuration.OcelotJRoutes();
JObject jGlobal = configuration.OcelotJGlobalConfiguration();
baseUrl = configuration.OcelotJGlobalConfiguration()[nameof(FileGlobalConfiguration.BaseUrl)].Value<string>(); // JToken expression
baseUrl = configuration.OcelotJGlobalProperty<string>(nameof(FileGlobalConfiguration.BaseUrl));
defaultCountry = configuration.OcelotJGlobalProperty<string>("DefaultCountry");
defaultCities = configuration.OcelotJGlobalProperty<string[]>("DefaultCities"); // custom array
defaults = configuration.OcelotJGlobalProperty<DefaultInfo>("Defaults"); // custom options class

JObject jWeather = configuration.OcelotJRoute("/weather/current/{city}"); // get by UpstreamPathTemplate, case insensitive
JToken jWeatherDefCity = jWeather["DefaultCity"];
weatherDefaultCity = jWeather["DefaultCity"].Value<string>(); // getting value via JToken of the route
weatherDefaultCity = configuration.OcelotJProperty<string>("DefaultCity", "/weather/current/{city}"); // search route + get property JToken + cast value to return
weatherDefaultCountry = configuration.OcelotJProperty<string>("DefaultCountry", "/weather/current/{city}");

var jPosts = configuration.OcelotJRoute(key: "R3"); // get by Key, case insensitive, thus 'R3' and 'r3' are the same routes
postsDefaultCountry = configuration.OcelotJProperty<string>("DefaultCountry", key: "R3"); // the route has no custom properties, thus, we read global property
#endregion

builder.Services
    .AddOcelot(configuration);

// Add your features
if (builder.Environment.IsDevelopment())
    builder.Logging.AddConsole();

int totalRoutes = ocRoutes.GetChildren().Count();
totalRoutes = jRoutes.Children().Count();
Console.Out
    .Welcome(out int pageWidth, "Welcome to the Ocelot Configuration app!", 7)
    .Print($"Detected {totalRoutes} routes");
foreach (JToken jRt in jRoutes)
    Console.Out.Print($"{jRoutes.IndexOf(jRt) + 1}. {jRt[nameof(FileRoute.UpstreamPathTemplate)]}", 3);
Console.Out
    .PageBreak(pageWidth)
    .Print("Detecting custom properties in route JSON schema...");
for (int i = 0; i < totalRoutes; i++)
{
    var jRt = jRoutes[i] as JObject;
    var customProps = jRt.OcelotCustomProperties<FileRoute>();
    var line = $"{jRt[nameof(FileRoute.UpstreamPathTemplate)]} route has " +
        (customProps.Count > 0 ? "custom properties:" : "no custom properties.");
    Console.Out.Print(line, 3, color: customProps.Count > 0 ? ConsoleColor.Green : ConsoleColor.Gray);
    foreach (JProperty jp in customProps)
        Console.Out.Print($"{jp.Name} -> {jp.Value}", 6, color: ConsoleColor.Yellow, resetColor: true);
}
Console.Out
    .PageBreak(pageWidth)
    .Print("Detecting custom properties in global configuration JSON schema...");
var customGlobal = jGlobal.OcelotCustomProperties<FileGlobalConfiguration>();
Console.Out.Print($"Global configuration has " + (customGlobal.Count > 0 ? "custom properties:" : "no custom properties."),
    color: customGlobal.Count > 0 ? ConsoleColor.Green : ConsoleColor.Gray);
foreach (JProperty jp in customGlobal)
    Console.Out.Print($"{jp.Name} -> {jp.Value.ToString(Formatting.None)}", 3, color: ConsoleColor.Yellow, resetColor: true);
Console.Out.PageBreak(pageWidth);

// Add middlewares aka app.Use*()
var app = builder.Build();

await app.UseOcelot();
await app.RunAsync();
