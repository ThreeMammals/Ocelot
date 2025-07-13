using Microsoft.AspNetCore.Http;
using Moq.Protected;
using Ocelot.Requester;

namespace Ocelot.UnitTests.Requester
{
    public class SseDelegatingHandlerTests
    {
        [Fact]
        public async Task SendAsync_ForNonSseRequest_CallsBaseHandler()
        {
            // Arrange
            var mockHttpContext = new DefaultHttpContext();
            var mockAccessor = new Mock<IHttpContextAccessor>();
            mockAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext);

            var mockInnerHandler = new Mock<HttpMessageHandler>();
            mockInnerHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var handler = new SseDelegatingHandler(mockAccessor.Object)
            {
                InnerHandler = mockInnerHandler.Object
            };

            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            mockInnerHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
    }

}
