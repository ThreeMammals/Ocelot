Websockets
==========

    * `WebSockets Standard <https://websockets.spec.whatwg.org/>`_ by WHATWG organization
    * `The WebSocket Protocol <https://datatracker.ietf.org/doc/html/rfc6455>`_ by Internet Engineering Task Force (IETF) organization

Ocelot supports proxying `WebSockets <https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API>`_ [#f1]_ with some extra bits.

Configuration
-------------

To enable *WebSockets* proxying with Ocelot, you need to do the following in your `Program`_:

.. code-block:: csharp
  :emphasize-lines: 2

  var app = builder.Build();
  app.UseWebSockets();
  await app.UseOcelot();
  await app.RunAsync();

Then, in your `ocelot.json`_, add the following to proxy a route using *WebSockets*:

.. code-block:: json
  :emphasize-lines: 4

  {
    "UpstreamPathTemplate": "/",
    "DownstreamPathTemplate": "/ws",
    "DownstreamScheme": "ws",
    "DownstreamHostAndPorts": [
      { "Host": "localhost", "Port": 5001 }
    ]
  }

With this configuration, Ocelot will match any *WebSockets* traffic that comes in on / and proxy it to ``localhost:5001/ws``.
For clarity, Ocelot will receive messages from the upstream client, proxy them to the downstream service, receive messages from the downstream service, and then proxy them back to the upstream client.

Handy Links
-----------

* WHATWG: `WebSockets Standard <https://websockets.spec.whatwg.org/>`_
* Mozilla Developer Network: `The WebSocket API (WebSockets) <https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API>`_
* Microsoft Learn: `WebSockets support in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets>`_
* Microsoft Learn: `WebSockets support in .NET <https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/websockets>`_

.. _ws-signalr:

SignalR [#f2]_
--------------

  Welcome to `Real-time ASP.NET with SignalR <https://dotnet.microsoft.com/en-us/apps/aspnet/signalr>`_

Ocelot supports proxying *SignalR*. To enable this with Ocelot, you need to do the following:

First, install the `SignalR Client <https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client>`_ NuGet package:

.. code-block:: powershell

  Install-Package Microsoft.AspNetCore.SignalR.Client

.. _break: http://break.do

  **Note**: SignalR is `part of the ASP.NET Core <https://github.com/dotnet/aspnetcore/tree/main/src/SignalR>`_ and can be referenced as follows:

  .. code-block:: xml

    <ItemGroup>
      <FrameworkReference Include="Microsoft.AspNetCore.App" />
    </ItemGroup>

  More information on framework compatibility can be found in the instructions: `Use ASP.NET Core APIs in a class library <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/target-aspnetcore>`_.

Second, you need to configure your application to use *SignalR*.
A complete reference can be found here: `ASP.NET Core SignalR configuration <https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration>`_.

.. code-block:: csharp

  builder.Services.AddOcelot(builder.Configuration);
  builder.Services.AddSignalR();

.. _break2: http://break.do

  **Note**: Make sure to pay attention to the transport-level configuration for *WebSockets*.
  Ensure that allowed transports are properly configured to enable *WebSockets* connections: `ASP.NET Core SignalR configuration <https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration>`_.

Next, include the following in your `ocelot.json`_ file to proxy a route using *SignalR*.
Note that standard Ocelot routing rules apply; the key aspect is that the scheme is set to ``ws`` (*WebSockets*).

.. code-block:: json
  :emphasize-lines: 4

  {
    "UpstreamPathTemplate": "/gateway/{catchAll}",
    "DownstreamPathTemplate": "/{catchAll}",
    "DownstreamScheme": "ws",
    "DownstreamHostAndPorts": [
      { "Host": "localhost", "Port": 5001 }
    ]
  }

.. _ws-secure:

WebSocket Secure
----------------

If you define a route with the *secured WebSockets* protocol, use the ``wss`` scheme:

.. code-block:: json

  "DownstreamScheme": "wss",

Keep in mind that you can use WebSocket SSL for both :ref:`SignalR <ws-signalr>` and :doc:`../features/websockets`.

  **Note**: To understand ``wss`` scheme, browse to this documentation:

  * IETF | The WebSocket Protocol: `WebSocket URIs <https://datatracker.ietf.org/doc/html/rfc6455#section-3>`_
  * Microsoft Learn: `Secure your connection with TLS/SSL <https://learn.microsoft.com/en-us/windows/uwp/networking/websockets#secure-your-connection-with-tlsssl>`_
  * Microsoft Learn: `Search for "secure websocket" <https://learn.microsoft.com/en-us/search/?terms=secure%20websocket>`_

If you want to ignore SSL warnings (errors) [#f3]_, configure your route as follows:

.. code-block:: json

  "DownstreamScheme": "wss",
  "DangerousAcceptAnyServerCertificateValidator": true,

*However, we strongly advise against this!*
Refer to the official notes regarding :ref:`ssl-errors` in the :doc:`../features/configuration` documentation.
There, you can also explore best practices tailored for your environments.

Supported
---------

1. :doc:`../features/routing`
2. :doc:`../features/loadbalancer`
3. :doc:`../features/servicediscovery`

This means you can configure your downstream services to run *WebSockets* and either:

* Include multiple ``DownstreamHostAndPorts`` in your route configuration.
* Connect your route to a :doc:`../features/servicediscovery` provider.
  This allows you to load balance requests, which we think is pretty cool!

Not Supported
-------------

Unfortunately, many Ocelot features are not specific to *WebSockets*, such as header handling and HTTP client functionalities.
Below is a list of features that will not work:

1. :doc:`../features/tracing`
2. :doc:`../features/logging` :ref:`lg-request-id`
3. :doc:`../features/aggregation`
4. :doc:`../features/ratelimiting`
5. :doc:`../features/qualityofservice`
6. :doc:`../features/middlewareinjection`
7. :doc:`../features/headerstransformation`
8. :doc:`../features/delegatinghandlers`
9. :doc:`../features/claimstransformation`
10. :doc:`../features/caching`
11. :doc:`../features/authentication` [#f4]_
12. :doc:`../features/authorization`

We cannot be entirely sure how this feature will behave once it is widely used. Therefore, thorough testing is strongly recommended!

Roadmap
-------

*WebSockets* and *SignalR* are being actively developed by the .NET community.
It is important to stay updated with trends and regularly check for new releases in the official documentation:

* `WebSockets docs <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets>`_
* `SignalR docs <https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction>`_

As a team, we are unable to provide direct development advice.
However, feel free to ask questions or explore coding recipes in `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ of the repository.
Additionally, we welcome any bug reports, enhancement suggestions, or proposals related to this feature. |octocat|

.. note::
  The Ocelot team considers the current implementation of the *WebSockets* feature to be obsolete, as it is based on the `WebSocketsProxyMiddleware <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20WebSocketsProxyMiddleware&type=code>`_ class.
  *WebSockets* are a part of the ASP.NET Core framework, which includes the native `WebSocketMiddleware <https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.websockets.websocketmiddleware>`_ class.
  We have a strong intention to either migrate or redesign this feature. For more details, see issue `1707`_.

""""

.. [#f1] The :doc:`../features/websockets` functionality was requested in issue `212 <https://github.com/ThreeMammals/Ocelot/issues/212>`_ and introduced in version `5.3.0`_.
.. [#f2] The :ref:`SignalR <ws-signalr>` functionality was requested in issue `344`_ and published in version `8.0.7`_.
.. [#f3] The ":ref:`ws-secure`"  feature includes a ``wss`` scheme fake validator, which was introduced in pull request `1377`_ as part of issues `1375`_, `1237`_, and others.
  This "life hack" for self-signed SSL certificates is available starting from version `20.0`_.
  However, it will be either removed or reworked in future releases. For further details, refer to the :ref:`ssl-errors` section.
.. [#f4] If requested, we might explore options for implementing basic authentication.

.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/Program.cs
.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/ocelot.json

.. _212: https://github.com/ThreeMammals/Ocelot/issues/212
.. _344: https://github.com/ThreeMammals/Ocelot/issues/344
.. _1237: https://github.com/ThreeMammals/Ocelot/issues/1237
.. _1375: https://github.com/ThreeMammals/Ocelot/issues/1375
.. _1377: https://github.com/ThreeMammals/Ocelot/pull/1377
.. _1707: https://github.com/ThreeMammals/Ocelot/issues/1707
.. _5.3.0: https://github.com/ThreeMammals/Ocelot/releases/tag/5.3.0
.. _8.0.7: https://github.com/ThreeMammals/Ocelot/releases/tag/8.0.7
.. _20.0: https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0

.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :height: 25
  :class: img-valign-middle
