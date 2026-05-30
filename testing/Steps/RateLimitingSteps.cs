using Microsoft.AspNetCore.Http;

namespace Ocelot.Testing.Steps;

public class RateLimitingSteps : AcceptanceSteps
{
    public Task<HttpResponseMessage[]> WhenIGetUrlOnTheApiGatewayMultipleTimes(string url, int times)
        => WhenIGetUrlOnTheApiGatewayMultipleTimesWithRateLimitingByAHeader(url, times);

    protected HttpResponseMessage[]? Responses;

    public async Task<HttpResponseMessage[]> WhenIGetUrlOnTheApiGatewayMultipleTimesWithRateLimitingByAHeader(
        string url, int times, string clientIdHeader = "ClientId", string clientIdHeaderValue = "ocelotclient1")
    {
        List<Task<HttpResponseMessage>> tasks = new();
        for (var i = 0; i < times; i++)
        {
            var request = new HttpRequestMessage(new(HttpMethods.Get), url);
            request.Headers.Add(clientIdHeader, clientIdHeaderValue);
            tasks.Add(ocelotClient!.SendAsync(request, CancelMe));
        }
        Responses = await Task.WhenAll(tasks);
        response = Responses.Last();
        return Responses;
    }
}
