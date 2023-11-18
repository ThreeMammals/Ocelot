Authentication
==============

In order to authenticate Routes and subsequently use any of Ocelot's claims based features such as authorization or modifying the request with values from the token,
users must register authentication services in their **Startup.cs** as usual but they provide a scheme (authentication provider key) with each registration e.g.

.. code-block:: csharp

    public void ConfigureServices(IServiceCollection services)
    {
        var authenticationProviderKey = "TestKey";
        services
            .AddAuthentication()
            .AddJwtBearer(authenticationProviderKey,
                options => { /* custom auth-setup */ });
    }

In this example "**TestKey**" is the scheme that this provider has been registered with. We then map this to a Route in the configuration e.g.

.. code-block:: json

  "Routes": [{
    "AuthenticationOptions": {
      "AuthenticationProviderKey": "TestKey",
      "AllowedScopes": []
    }
  }]

When Ocelot runs it will look at this Routes ``AuthenticationOptions.AuthenticationProviderKey`` and check that there is an authentication provider registered with the given key.
If there isn't then Ocelot will not start up. If there is then the Route will use that provider when it executes.

If a Route is authenticated, Ocelot will invoke whatever scheme is associated with it while executing the authentication middleware.
If the request fails authentication, Ocelot returns a HTTP status code `401 Unauthorized <https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/401>`_.

JWT Tokens
----------

If you want to authenticate using JWT tokens maybe from a provider like `Auth0 <https://auth0.com/>`_, you can register your authentication middleware as normal e.g.

.. code-block:: csharp

    public void ConfigureServices(IServiceCollection services)
    {
        var authenticationProviderKey = "TestKey";
        services
            .AddAuthentication()
            .AddJwtBearer(authenticationProviderKey, options =>
            {
                options.Authority = "test";
                options.Audience = "test";
            });
        services.AddOcelot();
    }

Then map the authentication provider key to a Route in your configuration e.g.

.. code-block:: json

  "Routes": [{
    "AuthenticationOptions": {
      "AuthenticationProviderKey": "TestKey",
      "AllowedScopes": []
    }
  }]

Identity Server Bearer Tokens
-----------------------------

In order to use `IdentityServer <https://github.com/IdentityServer>`_ bearer tokens, register your IdentityServer services as usual in ``ConfigureServices`` with a scheme (key).
If you don't understand how to do this, please consult the IdentityServer `documentation <https://identityserver4.readthedocs.io/>`_.

.. code-block:: csharp

    public void ConfigureServices(IServiceCollection services)
    {
        var authenticationProviderKey = "TestKey";
        Action<JwtBearerOptions> options = (opt) =>
        {
            opt.Authority = "https://whereyouridentityserverlives.com";
            // ...
        };
        services
            .AddAuthentication()
            .AddJwtBearer(authenticationProviderKey, options);
        services.AddOcelot();
    }

Then map the authentication provider key to a Route in your configuration e.g.

.. code-block:: json

  "Routes": [{
    "AuthenticationOptions": {
      "AuthenticationProviderKey": "TestKey",
      "AllowedScopes": []
    }
  }]

Auth0 by Okta
-------------
Yet another identity provider by `Okta <https://www.okta.com/>`_, see `Auth0 Developer Resources <https://developer.auth0.com/>`_.

Add the following to your startup ``Configure`` method:

.. code-block:: csharp

    app.UseAuthentication()
        .UseOcelot().Wait();

Add the following, at minimum, to your startup ``ConfigureServices`` method:

.. code-block:: csharp

    services
        .AddAuthentication()
        .AddJwtBearer(oktaProviderKey, options =>
        {
            options.Audience = configuration["Authentication:Okta:Audience"]; // Okta Authorization server Audience
            options.Authority = configuration["Authentication:Okta:Server"]; // Okta Authorization Issuer URI URL e.g. https://{subdomain}.okta.com/oauth2/{authidentifier}
        });
    services.AddOcelot(configuration);

**Note** In order to get Ocelot to view the scope claim from Okta properly, you have to add the following to map the default Okta ``"scp"`` claim to ``"scope"``:

.. code-block:: csharp

    // Map Okta "scp" to "scope" claims instead of http://schemas.microsoft.com/identity/claims/scope to allow Ocelot to read/verify them
    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("scp");
    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Add("scp", "scope");

`Issue 446 <https://github.com/ThreeMammals/Ocelot/issues/446>`_ contains some code and examples that might help with Okta integration.

Allowed Scopes
--------------

If you add scopes to **AllowedScopes**, Ocelot will get all the user claims (from the token) of the type scope and make sure that the user has at least one of the scopes in the list.

This is a way to restrict access to a Route on a per scope basis.

More identity providers
-----------------------

We invite you to add more examples, if you have integrated with other identity providers and the integration solution is working.
Please, open `Show and tell <https://github.com/ThreeMammals/Ocelot/discussions/categories/show-and-tell>`_ discussion in the repository.
