Authentication
==============

In order to authenticate Routes and subsequently use any of Ocelot's claims based features such as authorization or modifying the request with values from the token,
users must register authentication services in their **Startup.cs** as usual but they provide `a scheme <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/#authentication-scheme>`_ 
(authentication provider key) with each registration e.g.

.. code-block:: csharp

    public void ConfigureServices(IServiceCollection services)
    {
        const string AuthenticationProviderKey = "MyKey";
        services
            .AddAuthentication()
            .AddJwtBearer(AuthenticationProviderKey, options =>
            {
                // Custom Authentication setup via options initialization
            });
    }

In this example ``MyKey`` is `the scheme <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/#authentication-scheme>`_ that this provider has been registered with.
We then map this to a Route in the configuration using the following `AuthenticationOptions <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20AuthenticationOptions&type=code>`_ properties:

* ``AuthenticationProviderKey`` is a string object, obsolete [#f1]_. This is legacy definition when you define :ref:`authentication-single`.
* ``AuthenticationProviderKeys`` is an array of strings, the recommended definition of :ref:`authentication-multiple` feature.

.. _authentication-single:

Single Key aka Authentication Scheme [#f1]_
-------------------------------------------

    | Property: ``AuthenticationOptions.AuthenticationProviderKey``

We map authentication provider to a Route in the configuration e.g.

.. code-block:: json

  "AuthenticationOptions": {
    "AuthenticationProviderKey": "MyKey",
    "AllowedScopes": []
  }

When Ocelot runs it will look at this Routes ``AuthenticationProviderKey`` and check that there is an authentication provider registered with the given key.
If there isn't then Ocelot will not start up. If there is then the Route will use that provider when it executes.

If a Route is authenticated, Ocelot will invoke whatever scheme is associated with it while executing the authentication middleware.
If the request fails authentication, Ocelot returns a HTTP status code `401 Unauthorized <https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/401>`_.

.. _authentication-multiple:

Multiple Authentication Schemes [#f2]_
--------------------------------------

    | Property: ``AuthenticationOptions.AuthenticationProviderKeys``

In real world of ASP.NET, apps may need to support multiple types of authentication by single Ocelot app instance.
To register `multiple authentication schemes <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme#use-multiple-authentication-schemes>`_
(`authentication provider keys <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20AuthenticationProviderKey&type=code>`_) for each appropriate authentication provider, use and develop this abstract configuration of two or more schemes:

.. code-block:: csharp

    public void ConfigureServices(IServiceCollection services)
    {
        const string DefaultScheme = JwtBearerDefaults.AuthenticationScheme; // Bearer
        services.AddAuthentication()
            .AddJwtBearer(DefaultScheme, options => { /* JWT setup */ })
            // AddJwtBearer, AddCookie, AddIdentityServerAuthentication etc. 
            .AddMyProvider("MyKey", options => { /* Custom auth setup */ });
    }

In this example, the ``MyKey`` and ``Bearer`` schemes represent the keys with which these providers were registered.
We then map these schemes to a Route in the configuration as shown below.

.. code-block:: json

  "AuthenticationOptions": {
    "AuthenticationProviderKeys": [ "Bearer", "MyKey" ] // The order matters!
    "AllowedScopes": []
  }

Afterward, Ocelot applies all steps that are specified for ``AuthenticationProviderKey`` as :ref:`authentication-single`.

**Note** that the order of the keys in an array definition does matter! We use a "First One Wins" authentication strategy.

Finally, we would say that registering providers, initializing options, forwarding authentication artifacts can be a "real" coding challenge.
If you're stuck or don't know what to do, just find inspiration in our `acceptance tests <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+MultipleAuthSchemesFeatureTests+language%3AC%23&type=code&l=C%23>`_
(currently for `Identity Server 4 <https://identityserver4.readthedocs.io/>`_ only) [#f3]_.

JWT Tokens
----------

If you want to authenticate using JWT tokens maybe from a provider like `Auth0 <https://auth0.com/>`_, you can register your authentication middleware as normal e.g.

.. code-block:: csharp

    public void ConfigureServices(IServiceCollection services)
    {
        var authenticationProviderKey = "MyKey";
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

  "AuthenticationOptions": {
    "AuthenticationProviderKeys": [ "MyKey" ],
    "AllowedScopes": []
  }

Docs
^^^^

* Microsoft Learn: `Authentication and authorization in minimal APIs <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security>`_
* Andrew Lock | .NET Escapades: `A look behind the JWT bearer authentication middleware in ASP.NET Core <https://andrewlock.net/a-look-behind-the-jwt-bearer-authentication-middleware-in-asp-net-core/>`_

Identity Server Bearer Tokens
-----------------------------

In order to use `IdentityServer <https://github.com/IdentityServer>`_ bearer tokens, register your IdentityServer services as usual in ``ConfigureServices`` with a scheme (key).
If you don't understand how to do this, please consult the IdentityServer `documentation <https://identityserver4.readthedocs.io/>`_.

.. code-block:: csharp

    public void ConfigureServices(IServiceCollection services)
    {
        var authenticationProviderKey = "MyKey";
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

  "AuthenticationOptions": {
    "AuthenticationProviderKeys": [ "MyKey" ],
    "AllowedScopes": []
  }

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

Links
-----

* Microsoft Learn: `Overview of ASP.NET Core authentication <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/>`_
* Microsoft Learn: `Authorize with a specific scheme in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme>`_
* Microsoft Learn: `Policy schemes in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/policyschemes>`_
* Microsoft .NET Blog: `ASP.NET Core Authentication with IdentityServer4 <https://devblogs.microsoft.com/dotnet/asp-net-core-authentication-with-identityserver4/>`_

Future
------

We invite you to add more examples, if you have integrated with other identity providers and the integration solution is working.
Please, open `Show and tell <https://github.com/ThreeMammals/Ocelot/discussions/categories/show-and-tell>`_ discussion in the repository.

""""

.. [#f1] Use the ``AuthenticationProviderKeys`` property instead of ``AuthenticationProviderKey`` one. We support this ``[Obsolete]`` property for backward compatibility and migration reasons. In future releases, the property may be removed as a breaking change.
.. [#f2] "`Multiple authentication schemes <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme#use-multiple-authentication-schemes>`__" feature was requested in issues `740 <https://github.com/ThreeMammals/Ocelot/issues/740>`_, `1580 <https://github.com/ThreeMammals/Ocelot/issues/1580>`_ and delivered as a part of `23.0 <https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0>`_ release.
.. [#f3] We would appreciate any new PRs to add extra acceptance tests for your custom scenarios with `multiple authentication schemes <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme#use-multiple-authentication-schemes>`__.
