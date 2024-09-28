using BenchmarkDotNet.Order;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace Ocelot.Benchmarks;

[Config(typeof(PayloadBenchmarks))]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PayloadBenchmarks : ManualConfig
{
    private IWebHost _service;
    private IWebHost _ocelot;
    private HttpClient _httpClient;

    private const string BasePayload =
        "{\"_id\":\"65789c1611a3b1feb49f9e65\",\"index\":0,\"guid\":\"6622d724-c17d-4939-9c68-158bf2dc5c57\",\"isActive\":false,\"balance\":\"$1,398.26\",\"picture\":\"http://placehold.it/32x32\",\"age\":33,\"eyeColor\":\"blue\",\"name\":\"WilkersonPayne\",\"gender\":\"male\",\"company\":\"NEOCENT\",\"email\":\"wilkersonpayne@neocent.com\",\"phone\":\"+1(837)588-3248\",\"address\":\"932BatchelderStreet,Campo,Texas,1310\",\"about\":\"Dolorsuntminimnullatemporlaboretempornostrudnon.Irureconsectetursintenimestadduissunttemporquisnisi.Laboreoccaecatculpaaliquaipsumreprehenderitadofficia.Sunteuutinpariaturanimofficia.CommodosintLoremametincididuntvelitesse.Nonaliquasintdoeiusmodexercitation.Suntcommododolorcupidatatculpareprehenderitfugiatexquisamet.\\r\\n\",\"registered\":\"2021-09-06T11:54:41-02:00\",\"latitude\":-45.256336,\"longitude\":164.343713,\"tags\":[\"cillum\",\"cupidatat\",\"aliquip\",\"culpa\",\"non\",\"laboris\",\"non\"],\"friends\":[{\"id\":0,\"name\":\"MistyMorton\"},{\"id\":1,\"name\":\"AraceliAcosta\"},{\"id\":2,\"name\":\"WalterDelaney\"}],\"greeting\":\"Hello,WilkersonPayne!Youhave1unreadmessages.\",\"favoriteFruit\":\"strawberry\"}";

    public PayloadBenchmarks()
    {
        AddColumn(StatisticColumn.AllStatistics);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddValidator(BaselineValidator.FailOnError);
    }

    [GlobalSetup]
    public void SetUp()
    {
        var configuration = new FileConfiguration
        {
            Routes = new()
            {
                new FileRoute
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort("localhost", 51879),
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new() { "Post" },
                },
            },
        };

        GivenThereIsAServiceRunningOn("http://localhost:51879", "/", 201);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning("http://localhost:5000");

        _httpClient = new HttpClient();
    }

    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(Payloads))]
    public async Task Baseline(string payLoadPath, string payloadName, bool isJson)
    {
        using var content = new StreamContent(File.OpenRead(payLoadPath));
        content.Headers.ContentType =
            new MediaTypeHeaderValue(string.Concat("application/", isJson ? "json" : "octet-stream"));

        var response = await _httpClient.PostAsync("http://localhost:5000/", content);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Generating the payloads for the benchmarks dynamically.
    /// </summary>
    /// <returns>The payloads containing path, file name and a boolean indicating if the file is a json or not.</returns>
    public static IEnumerable<object[]> Payloads()
    {
        var baseDirectory = GetBaseDirectory();
        var payloadsDirectory = Path.Combine(baseDirectory, nameof(Payloads));

        if (!Directory.Exists(payloadsDirectory))
        {
            Directory.CreateDirectory(payloadsDirectory);
        }

        // Array of sizes in kilobytes for JSON files
        var jsonSizes = new[] { 1, 16, 32, 64, 128, 256, 512, 2 * 1024, 8 * 1024, 15 * 1024, 30 * 1024 };
        foreach (var size in jsonSizes)
        {
            yield return GeneratePayload(size, payloadsDirectory, $"{size}KBPayload.json", true);
        }

        // Array of sizes in megabytes for DAT files
        var datSizes = new[] { 10, 100, 1024 };
        foreach (var size in datSizes)
        {
            yield return GeneratePayload(size, payloadsDirectory, $"{size}MBPayload.dat", false);
        }
    }

    private static string GetBaseDirectory()
    {
        var baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        Debug.Assert(baseDirectory != null, nameof(baseDirectory) + " != null");
        return baseDirectory;
    }

    private static object[] GeneratePayload(int size, string directory, string fileName, bool isJson)
    {
        var filePath = Path.Combine(directory, fileName);
        var generateDummy = isJson ? (Func<int, string, string>) GenerateDummyJsonFile : GenerateDummyDatFile;
        return new object[]
        {
            generateDummy(size, filePath),
            fileName,
            isJson,
        };
    }

    /// <summary>
    /// Generates a dummy payload of the given size in KB.
    /// The payload is a JSON array of the given size.
    /// </summary>
    /// <param name="sizeInKb">The size in KB.</param>
    /// <param name="payloadPath">The payload path.</param>
    /// <returns>The current payload path.</returns>
    private static string GenerateDummyJsonFile(int sizeInKb, string payloadPath)
    {
        ArgumentNullException.ThrowIfNull(payloadPath);

        if (File.Exists(payloadPath))
        {
            return payloadPath;
        }

        var targetSizeInBytes = sizeInKb * 1024L;

        using var fileStream = new FileStream(payloadPath, FileMode.Create, FileAccess.Write);
        using var streamWriter = new StreamWriter(fileStream);

        var byteArrayLength = Encoding.UTF8.GetBytes(BasePayload).Length;
        var firstObject = true;

        streamWriter.Write("[");
        while (fileStream.Length < targetSizeInBytes - byteArrayLength)
        {
            if (!firstObject)
            {
                streamWriter.Write(",");
            }
            else
            {
                firstObject = false;
            }

            streamWriter.Write(BasePayload);
        }

        streamWriter.Write("]");

        return payloadPath;
    }

    /// <summary>
    /// Generates a dummy payload of the given size in MB.
    /// Avoiding maintaining a large file in the repository.
    /// </summary>
    /// <param name="sizeInMb">The file size in MB.</param>
    /// <param name="payloadPath">The path to the payload file.</param>
    /// <returns>The payload file path.</returns>
    /// <exception cref="ArgumentNullException">Throwing an exception if the payload path is null.</exception>
    private static string GenerateDummyDatFile(int sizeInMb, string payloadPath)
    {
        ArgumentNullException.ThrowIfNull(payloadPath);

        if (File.Exists(payloadPath))
        {
            return payloadPath;
        }

        using var newFile = new FileStream(payloadPath, FileMode.CreateNew);
        newFile.Seek(sizeInMb * 1024L * 1024, SeekOrigin.Begin);
        newFile.WriteByte(0);
        newFile.Close();

        return payloadPath;
    }

    private void GivenOcelotIsRunning(string url)
    {
        _ocelot = new WebHostBuilder()
            .UseKestrel()
            .UseUrls(url)
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config
                    .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json", true, true)
                    .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                    .AddJsonFile(ConfigurationBuilderExtensions.PrimaryConfigFile, false, false)
                    .AddEnvironmentVariables();
            })
            .ConfigureKestrel((_, hostingOptions) => { hostingOptions.Limits.MaxRequestBodySize = 2684354561; })
            .ConfigureServices(s => { s.AddOcelot(); })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
            })
            .Configure(async app => { await app.UseOcelot(); })
            .Build();

        _ocelot.Start();
    }

    public static void GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
    {
        var configurationPath = Path.Combine(AppContext.BaseDirectory, ConfigurationBuilderExtensions.PrimaryConfigFile);
        var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

        if (File.Exists(configurationPath))
        {
            File.Delete(configurationPath);
        }

        File.WriteAllText(configurationPath, jsonConfiguration);
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode)
    {
        _service = new WebHostBuilder()
            .UseUrls(baseUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureKestrel((_, hostingOptions) => { hostingOptions.Limits.MaxRequestBodySize = 2684354561; })
            .Configure(app =>
            {
                app.UsePathBase(basePath);
                app.Run(async context =>
                {
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync(string.Empty);
                });
            })
            .Build();

        _service.Start();
    }
}
