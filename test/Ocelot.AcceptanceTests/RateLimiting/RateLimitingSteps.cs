using Microsoft.AspNetCore.Http;

namespace Ocelot.AcceptanceTests.RateLimiting;

public class RateLimitingSteps : Steps
{
    public Task<HttpResponseMessage[]> WhenIGetUrlOnTheApiGatewayMultipleTimes(string url, int times)
        => WhenIGetUrlOnTheApiGatewayMultipleTimesWithRateLimitingByAHeader(url, times);

    public async Task<HttpResponseMessage[]> WhenIGetUrlOnTheApiGatewayMultipleTimesWithRateLimitingByAHeader(string url, int times,
        string clientIdHeader = "ClientId", string clientIdHeaderValue = "ocelotclient1")
    {
        List<Task<HttpResponseMessage>> tasks = new();
        for (var i = 0; i < times; i++)
        {
            var request = new HttpRequestMessage(new(HttpMethods.Get), url);
            request.Headers.Add(clientIdHeader, clientIdHeaderValue);
            tasks.Add(ocelotClient.SendAsync(request));
        }
        var responses = await Task.WhenAll(tasks);
        response = responses.Last();
        return responses;
    }
}
