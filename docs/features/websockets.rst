Websockets
==========

    * `WebSockets Standard <https://websockets.spec.whatwg.org/>`_ by WHATWG organization
    * `The WebSocket Protocol <https://datatracker.ietf.org/doc/html/rfc6455>`_ by Internet Engineering Task Force (IETF) organization

Ocelot supports proxying `WebSockets <https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API>`_ with some extra bits.
This functionality was requested in `issue 212 <https://github.com/ThreeMammals/Ocelot/issues/212>`_. 

In order to get *WebSocket* proxying working with Ocelot you need to do the following.
In your ``Configure`` method you need to tell your application to use *WebSockets*:

.. code-block:: csharp

    Configure(app =>
    {
        app.UseWebSockets();
        app.UseOcelot().Wait();
    })

Then in your **ocelot.json** add the following to proxy a Route using *WebSockets*:

.. code-block:: json

  {
    "UpstreamPathTemplate": "/",
    "DownstreamPathTemplate": "/ws",
    "DownstreamScheme": "ws",
    "DownstreamHostAndPorts": [
      { "Host": "localhost", "Port": 5001 }
    ]
  }

With this configuration set Ocelot will match any *WebSocket* traffic that comes in on / and proxy it to ``localhost:5001/ws``.
To make this clearer Ocelot will receive messages from the upstream client, proxy these to the downstream service, receive messages from the downstream service and proxy these to the upstream client.

Links
-----

* WHATWG: `WebSockets Standard <https://websockets.spec.whatwg.org/>`_
* Mozilla Developer Network: `The WebSocket API (WebSockets) <https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API>`_
* Microsoft Learn: `WebSockets support in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets>`_
* Microsoft Learn: `WebSockets support in .NET <https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/websockets>`_

SignalR
-------

    Welcome to `Real-time ASP.NET with SignalR <https://dotnet.microsoft.com/en-us/apps/aspnet/signalr>`_

Ocelot supports proxying *SignalR*.
This functionality was requested in `issue 344 <https://github.com/ThreeMammals/Ocelot/issues/344>`_. 
In order to get *WebSocket* proxying working with Ocelot you need to do the following.

**First**, install `SignalR Client <https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client>`_ NuGet package:

.. code-block:: powershell

    NuGet\Install-Package Microsoft.AspNetCore.SignalR.Client

The package is deprecated, but `new versions <https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client#versions-body-tab>`_ are still built from the source code.
So, SignalR is `the part <https://github.com/dotnet/aspnetcore/tree/main/src/SignalR>`_ of the ASP.NET Framework which can be referenced like:

.. code-block:: xml

    <ItemGroup>
      <FrameworkReference Include="Microsoft.AspNetCore.App" />
    </ItemGroup>

More information on framework compatibility can be found in instrictions: `Use ASP.NET Core APIs in a class library <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/target-aspnetcore>`_.

**Second**, you need to tell your application to use *SignalR*.
Complete reference is here: `ASP.NET Core SignalR configuration <https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration>`_

.. code-block:: csharp

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOcelot();
        services.AddSignalR();
    }

Pay attention to configuration of transport level of *WebSockets*,
so `configure allowed transports <https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration#configure-allowed-transports>`_ to allow *WebSockets* connections.

**Then** in your **ocelot.json** add the following to proxy a Route using SignalR.
Note normal Ocelot routing rules apply the main thing is the scheme which is set to ``ws``.

.. code-block:: json

  {
    "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE", "OPTIONS" ],
    "UpstreamPathTemplate": "/gateway/{catchAll}",
    "DownstreamPathTemplate": "/{catchAll}",
    "DownstreamScheme": "ws",
    "DownstreamHostAndPorts": [
      { "Host": "localhost", "Port": 5001 }
    ]
  }

WebSocket Secure
----------------

If you define a route with Secured WebSocket protocol, use the ``wss`` scheme:

.. code-block:: json

  {
    "DownstreamScheme": "wss",
    // ...
  }

Keep in mind: you can use WebSocket SSL for both `SignalR <#signalr>`_ and `WebSockets <#websockets>`__.

To understand ``wss`` scheme, browse to this:

* Microsoft Learn: `Secure your connection with TLS/SSL <https://learn.microsoft.com/en-us/windows/uwp/networking/websockets#secure-your-connection-with-tlsssl>`_
* IETF | The WebSocket Protocol: `WebSocket URIs <https://datatracker.ietf.org/doc/html/rfc6455#section-3>`_

If you have questions, it may be helpful to search for documentation on MS Learn:

* `Search for "secure websocket" <https://learn.microsoft.com/en-us/search/?terms=secure%20websocket>`_

SSL Errors
^^^^^^^^^^

If you want to ignore SSL warnings (errors), set the following in your Route config:

.. code-block:: json

  {
    "DownstreamScheme": "wss",
    "DangerousAcceptAnyServerCertificateValidator": true,
    // ...
  }

**But we don't recommend doing this!** Read the official notes regarding :ref:`ssl-errors` in the :doc:`../features/configuration` doc,
where you will also find best practices for your environments.

**Note**, the ``wss`` scheme fake validator was added by `PR 1377 <https://github.com/ThreeMammals/Ocelot/pull/1377>`_,
as a part of issues `1375 <https://github.com/ThreeMammals/Ocelot/issues/1375>`_, `1237 <https://github.com/ThreeMammals/Ocelot/issues/1237>`_ and etc.
This life hacking feature for self-signed SSL certificates is available in version `20.0 <https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0>`_.
It will be removed and/or reworked in future releases. See the :ref:`ssl-errors` section for details.

Supported
---------

1. :doc:`../features/loadbalancer`
2. :doc:`../features/routing`
3. :doc:`../features/servicediscovery`

This means that you can set up your downstream services running *WebSockets* and either have multiple **DownstreamHostAndPorts** in your Route config,
or hook your Route into a service discovery provider and then load balance requests... Which we think is pretty cool.

Not Supported
-------------

Unfortunately a lot of Ocelot features are non *WebSocket* specific, such as header and http client stuff.
We have listed what will not work below:

1. :doc:`../features/tracing`
2. :doc:`../features/requestid`
3. :doc:`../features/requestaggregation`
4. :doc:`../features/ratelimiting`
5. :doc:`../features/qualityofservice`
6. :doc:`../features/middlewareinjection`
7. :doc:`../features/headerstransformation`
8. :doc:`../features/delegatinghandlers`
9. :doc:`../features/claimstransformation`
10. :doc:`../features/caching`
11. :doc:`../features/authentication` [#f1]_
12. :doc:`../features/authorization`

We are not 100% sure what will happen with this feature when it gets into the wild, so please make sure you test thoroughly! 

Future
------

*Websockets* and *SignalR* are being developed intensively by the .NET community, so you need to watch for trends, releases in official docs regularly:

* `WebSockets docs <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets>`_
* `SignalR docs <https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction>`_

As a team, we cannot advise you on development,
but feel free to ask questions, get coding recipes in the `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ space of the repository. |octocat|

.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :width: 23

Also, we welcome any bug reports, enhancements or proposals regarding this feature.

The Ocelot team considers the current impementation of WebSockets feature obsolete, based on the `WebSocketsProxyMiddleware <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20WebSocketsProxyMiddleware&type=code>`_ class.
Websockets are the part of ASP.NET Core framework having native `WebSocketMiddleware <https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.websockets.websocketmiddleware>`_ class.
We have a strong intention to migrate or at least redesign the feature, see `issue 1707 <https://github.com/ThreeMammals/Ocelot/issues/1707>`_.

""""

.. [#f1] If anyone requests it, we might be able to do something with basic authentication.
