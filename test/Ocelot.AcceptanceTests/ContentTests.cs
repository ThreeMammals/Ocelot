using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using System.Diagnostics;

namespace Ocelot.AcceptanceTests
{
    public class ContentTests : IDisposable
    {
        private readonly Steps _steps;
        private string _contentType;
        private long? _contentLength;
        private long _memoryUsage;
        private bool _contentTypeHeaderExists;
        private readonly ServiceHandler _serviceHandler;

        public ContentTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
        }

        [Fact]
        public void should_not_add_content_type_or_content_length_headers()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 200, "Hello from Laura"))
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
        public void should_add_content_type_and_content_length_headers()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Post" },
                        },
                    },
            };

            var contentType = "application/json";

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 201, string.Empty))
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
        public void should_add_default_content_type_header()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Post" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 201, string.Empty))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenThePostHasContent("postContent"))
                .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
                .And(x => ThenTheContentTypeIsIs("text/plain; charset=utf-8"))
                .BDDfy();
        }

        [Fact]
        public void When_Downloading_File_Memory_Usage_Should_Not_Increase()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes =
                [
                    new FileRoute
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts =
                        [
                            new FileHostAndPort
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        ],
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod =["Get"],
                    },
                ],
            };

            this.Given(x => x.GivenThereIsAServiceWithPayloadRunningOn($"http://localhost:{port}", "/", 100))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => x.GivenTheCurrentMemoryUsage())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .Then(x => x.ThenMemoryUsageShouldNotIncrease())
                .BDDfy();
        }

        private void GivenTheCurrentMemoryUsage()
        {
            _memoryUsage = Process.GetCurrentProcess().WorkingSet64;
        }

        private void ThenMemoryUsageShouldNotIncrease()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            var currentMemoryUsage = Process.GetCurrentProcess().WorkingSet64;
            Assert.Equal(_memoryUsage, currentMemoryUsage);
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

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
            {
                _contentType = context.Request.ContentType;
                _contentLength = context.Request.ContentLength;
                _contentTypeHeaderExists = context.Request.Headers.TryGetValue("Content-Type", out var value);
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(responseBody);
            });
        }

        private void GivenThereIsAServiceWithPayloadRunningOn(string baseUrl, string basePath, int payloadSizeInMb)
        {
            var dummyDatFilePath = GenerateDummyDatFile(payloadSizeInMb);
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
            {
                context.Response.StatusCode = (int) HttpStatusCode.OK;

                await using var fileStream = File.OpenRead(dummyDatFilePath);
                await fileStream.CopyToAsync(context.Response.Body);
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

            using var newFile = new FileStream(payloadPath, FileMode.CreateNew);
            newFile.Seek(sizeInMb * 1024L * 1024, SeekOrigin.Begin);
            newFile.WriteByte(0);
            newFile.Close();

            return payloadPath;
        }

        public void Dispose()
        {
            _serviceHandler?.Dispose();
            _steps.Dispose();
        }
    }
}
