Authentication
==============

Users register authentication services in their Startup.cs as usual but they provide a scheme (key) with each registration e.g.

.. code-block:: csharp

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication()
            .AddJwtBearer("TestKey", x =>
            {
                x.Authority = "test";
                x.Audience = "test";
            });

        services.AddOcelot(Configuration);
    }


In this example TestKey is the scheme tha this provider has been registered with.
We then map this to a ReRoute in the configuration e.g.

.. code-block:: json

    "ReRoutes": [{
            "DownstreamPathTemplate": "/",
            "UpstreamPathTemplate": "/",
            "UpstreamHttpMethod": ["Post"],
            "ReRouteIsCaseSensitive": false,
            "DownstreamScheme": "http",
            "DownstreamHost": "localhost",
            "DownstreamPort": 51876,
            "AuthenticationOptions": {
                "AuthenticationProviderKey": "TestKey",
                "AllowedScopes": []
            }
        }]

When Ocelot runs it will look at this ReRoutes AuthenticationOptions.AuthenticationProviderKey 
and check that there is an Authentication provider registered with the given key. If there isn't then Ocelot 
will not start up, if there is then the ReRoute will use that provider when it executes.
