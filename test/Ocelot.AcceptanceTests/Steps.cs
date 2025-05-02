//using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.AcceptanceTests.Properties;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Net.Http.Headers;

namespace Ocelot.AcceptanceTests;

public class Steps : AcceptanceSteps
{
    private BearerToken _token;

    public Steps() : base()
    {
        BddfyConfig.Configure();
    }

    public void ThenTheResponseBodyHeaderIs(string key, string value)
    {
        var header = _response.Content.Headers.GetValues(key);
        header.First().ShouldBe(value);
    }

    public void ThenTheTraceHeaderIsSet(string key)
    {
        var header = _response.Headers.GetValues(key);
        header.First().ShouldNotBeNullOrEmpty();
    }

    public void GivenOcelotIsRunningWithDelegatingHandler<THandler>(bool global = false)
        where THandler : DelegatingHandler
    {
        GivenOcelotIsRunningWithServices(s => s
            .AddOcelot()
            .AddDelegatingHandler<THandler>(global));
    }

    public void GivenOcelotIsRunning(OcelotPipelineConfiguration pipelineConfig)
    {
        var builder = TestHostBuilder.Create() // ValidateScopes = true
            .ConfigureAppConfiguration(WithBasicConfiguration)
            .ConfigureServices(WithAddOcelot)
            .Configure(async a => await a.UseOcelot(pipelineConfig));
        _ocelotServer = new TestServer(builder);
        _ocelotClient = _ocelotServer.CreateClient();
    }

    public void GivenIHaveAddedATokenToMyRequest()
    {
        _ocelotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
    }

    public static List<KeyValuePair<string, string>> GivenDefaultAuthTokenForm() => new()
    {
        new ("client_id", "client"),
        new ("client_secret", "secret"),
        new ("scope", "api"),
        new ("username", "test"),
        new ("password", "test"),
        new ("grant_type", "password"),
    };

    internal Task<BearerToken> GivenIHaveAToken(string url)
    {
        var form = GivenDefaultAuthTokenForm();
        return GivenIHaveATokenWithForm(url, form);
    }

    internal async Task<BearerToken> GivenIHaveATokenWithForm(string url, IEnumerable<KeyValuePair<string, string>> form)
    {
        var tokenUrl = $"{url}/connect/token";
        var formData = form ?? Enumerable.Empty<KeyValuePair<string, string>>();
        var content = new FormUrlEncodedContent(formData);

        using var httpClient = new HttpClient();
        var response = await httpClient.PostAsync(tokenUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
        return _token;
    }
}
