using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using System.Diagnostics;

namespace Ocelot.AcceptanceTests
{
    public sealed class ContentTests : IDisposable
    {
        private string _contentType;
        private long? _contentLength;
        private long _memoryUsageAfterCallToService;
        private bool _contentTypeHeaderExists;

        private readonly ServiceHandler _serviceHandler;
        private readonly Steps _steps;

        public ContentTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
        }

        public void Dispose()
        {
            _serviceHandler.Dispose();
            _steps.Dispose();
        }

        [Fact]
        public void Should_Not_add_content_type_or_content_length_headers()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = GivenConfiguration(port);
            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(x => ThenTheContentTypeShouldBeEmpty())
                .And(x => ThenTheContentLengthShouldBeZero())
                .BDDfy();
        }

        [Fact]
        public void Should_add_content_type_and_content_length_headers()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = GivenConfiguration(port, HttpMethods.Post);
            var contentType = "application/json";
            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", HttpStatusCode.Created, string.Empty))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenThePostHasContent("postContent"))
                .And(x => _steps.GivenThePostHasContentType(contentType))
                .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
                .And(x => ThenTheContentTypeIsIs(contentType))
                .BDDfy();
        }

        [Fact]
        public void Should_add_default_content_type_header()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = GivenConfiguration(port, HttpMethods.Post);
            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", HttpStatusCode.Created, string.Empty))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenThePostHasContent("postContent"))
                .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
                .And(x => ThenTheContentTypeIsIs("text/plain; charset=utf-8"))
                .BDDfy();
        }

        [Fact]
        [Trait("PR", "1824")]
        [Trait("Issues", "356 695 1924")]
        public void Should_Not_increase_memory_usage_When_downloading_large_file()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = GivenConfiguration(port);
            var dummyDatFilePath = GenerateDummyDatFile(100);
            this.Given(x => x.GivenThereIsAServiceWithPayloadRunningOn($"http://localhost:{port}", "/", dummyDatFilePath))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .Then(x => x.ThenMemoryUsageShouldNotIncrease())
                .BDDfy();
        }

        private void ThenMemoryUsageShouldNotIncrease()
        {
            var currentMemoryUsage = Process.GetCurrentProcess().WorkingSet64;
            var tolerance = currentMemoryUsage - (10 * 1024 * 1024L);
            Assert.InRange(_memoryUsageAfterCallToService, currentMemoryUsage - tolerance, currentMemoryUsage + tolerance);
        }

        private void ThenTheContentTypeIsIs(string expected)
        {
            _contentType.ShouldBe(expected);
        }

        private void ThenTheContentLengthShouldBeZero()
        {
            _contentLength.ShouldBeNull();
        }

        private void ThenTheContentTypeShouldBeEmpty()
        {
            _contentType.ShouldBeNullOrEmpty();
            _contentTypeHeaderExists.ShouldBe(false);
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, HttpStatusCode statusCode, string responseBody)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
            {
                _contentType = context.Request.ContentType;
                _contentLength = context.Request.ContentLength;
                _contentTypeHeaderExists = context.Request.Headers.TryGetValue("Content-Type", out var value);
                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsync(responseBody);
            });
        }

        private void GivenThereIsAServiceWithPayloadRunningOn(string baseUrl, string basePath, string dummyDatFilePath)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await using var fileStream = File.OpenRead(dummyDatFilePath);
                await fileStream.CopyToAsync(context.Response.Body);
                _memoryUsageAfterCallToService = Process.GetCurrentProcess().WorkingSet64;
            });
        }

        /// <summary>
        /// Generates a dummy payload of the given size in MB.
        /// Avoiding maintaining a large file in the repository.
        /// </summary>
        /// <param name="sizeInMb">The file size in MB.</param>
        /// <returns>The payload file path.</returns>
        /// <exception cref="ArgumentNullException">Throwing an exception if the payload path is null.</exception>
        private static string GenerateDummyDatFile(int sizeInMb)
        {
            var payloadName = "dummy.dat";
            var payloadPath = Path.Combine(Directory.GetCurrentDirectory(), payloadName);

            if (File.Exists(payloadPath))
            {
                File.Delete(payloadPath);
            }

            var newFile = new FileStream(payloadPath, FileMode.CreateNew);
            try
            {
                newFile.Seek(sizeInMb * 1024L * 1024, SeekOrigin.Begin);
                newFile.WriteByte(0);
            }
            finally
            {
                newFile.Dispose();
            }

            return payloadPath;
        }

        private static FileConfiguration GivenConfiguration(int port, string method = null) => new()
        {
            Routes = new()
            {
                new FileRoute
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = Uri.UriSchemeHttp,
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort("localhost", port),
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new() {method ?? HttpMethods.Get },
                },
            },
        };
    }
}
