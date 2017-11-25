Authentication
==============


As part of the .net core 2.0 upgrade I have had to re-write a lot of the authentication stack.

Users now register authentication services in their Startup.cs as usual but they provide a scheme (key) with each registration e.g.

```csharp

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

```

In this example TestKey is the scheme tha this provider has been registered with.
We then map this to a ReRoute in the configuration e.g.

```json
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
```

When Ocelot runs it will look at this ReRoutes AuthenticationOptions.AuthenticationProviderKey 
and check that there is an Authentication provider registered with the given key. If there isn't then Ocelot 
will not start up, if there is then the ReRoute will use that provider when it executes.

### .net core 1.1

Ocelot currently supports the use of bearer tokens with Identity Server and normal JWTs such as Auth0. In order to identity a ReRoute as authenticated it needs the following
configuration added.

In this example the Provider is specified as IdentityServer. This string is important 
because it is used to identity the authentication provider (as previously mentioned in
the future there might be more providers). Identity server requires that the client
talk to it so we need to provide the root url of the IdentityServer as ProviderRootUrl.
IdentityServer requires at least one scope and you can also provider additional scopes.
Finally if you are using IdentityServer reference tokens you need to provide the scope
secret. 

```json
 "AuthenticationOptions": {
        "Provider": "IdentityServer",
        "AllowedScopes": [
          "some scope"
        ],
        "IdentityServerConfig": {
          "ProviderRootUrl": "http://localhost:51888",
          "ApiName": "api",
          "RequireHttps": false,
          "ApiSecret": "secret"
        }
      }
```

If you are just using JWTs for Auth0 then it will be the following..

```json
 "AuthenticationOptions": {
        "Provider": "Jwt",
        "AllowedScopes": [
          "some scope"
        ],
        "JwtConfig": {
          "Authority": "someAuthority..",
          "Audience": "someAudience..."
        }
      }
```

Ocelot will use this configuration to build an authentication handler and if 
authentication is succefull the next middleware will be called else the response
is 401 unauthorised.
