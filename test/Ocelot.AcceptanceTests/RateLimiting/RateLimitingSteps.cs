namespace Ocelot.AcceptanceTests.RateLimiting;

public class RateLimitingSteps : Steps
{
    public async Task WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(string url, int times)
    {
        for (var i = 0; i < times; i++)
        {
            const string clientId = "ocelotclient1";
            var request = new HttpRequestMessage(new HttpMethod("GET"), url);
            request.Headers.Add("ClientId", clientId);
            response = await ocelotClient.SendAsync(request);
        }
    }
}
