using IdentityServer4.Models;
using Microsoft.AspNetCore.Hosting;

namespace Ocelot.AcceptanceTests.Authentication;

public class AuthenticationSteps : Steps
{
    protected IWebHost CreateIdentityServer(string url, AccessTokenType tokenType, string[] apiScopes)
    {
        return null;
    }

    public async Task GivenIHaveATokenWithScope(string url, string apiScope = "api")
    {
        var form = GivenDefaultAuthTokenForm();
        form.RemoveAt(form.FindIndex(x => x.Key == "scope"));
        form.Add(new("scope", apiScope));

        await GivenIHaveATokenWithForm(url, form);
    }
}
