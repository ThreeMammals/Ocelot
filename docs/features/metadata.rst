Metadata
========

  [#f1]_ Feature of: :doc:`../features/configuration`

Ocelot provides various features such as routing, authentication, caching, load balancing, and more.
However, some users may encounter situations where Ocelot does not meet their specific needs or they want to customize its behavior.
In such cases, Ocelot allows users to add *metadata* to the route configuration.
This property can store any arbitrary data that users can access in middlewares or delegating handlers.

Schema
------

As you may already know from the :doc:`../features/configuration` chapter and the :ref:`config-route-metadata` section, the route *metadata* schema is quite simple which is JSON dictionary:

.. code-block:: json

  "Metadata": {
    // "key": "value",
  }

.. _FileMetadataOptions: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileMetadataOptions.cs

However, **global** metadata configuration consists of both the ``Metadata`` and ``MetadataOptions`` sections.
You do not need to set all of these things, but this is everything that is available at the moment.

.. code-block:: json

  "GlobalConfiguration": {
    "Metadata": {
      // "key": "value",
    },
    "MetadataOptions": {
      "CurrentCulture": "en-GB",
      "NumberStyle": "Any",
      "Separators": [","],
      "StringSplitOption": "None",
      "TrimChars": [" "],
    }
  }

The actual global *metadata* schema with all the properties can be found in the C# `FileMetadataOptions`_ class.
This configuration type is parsed to a `MetadataOptions <https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/MetadataOptions.cs>`_ type object.

.. list-table::
    :widths: 20 80
    :header-rows: 1

    * - *Option*
      - *Description*
    * - ``CurrentCulture``
      - | Parsed as the ``System.Globalization.CultureInfo`` object (refer to `CultureInfo <https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-globalization-cultureinfo>`_ class)
        | Default value is current culture aka ``CultureInfo.CurrentCulture.Name``
    * - ``NumberStyle``
      - | Parsed as the ``System.Globalization.NumberStyles`` object (refer to `NumberStyles <https://learn.microsoft.com/en-us/dotnet/api/system.globalization.numberstyles?view=net-9.0>`_ enum)
        | Default value is ``NumberStyles.Any``
    * - ``Separators``
      - Array of ``string``. Default value is ``[","]`` aka comma.
    * - ``StringSplitOption``
      - | Parsed as the ``System.StringSplitOptions`` object (refer to `StringSplitOptions <https://learn.microsoft.com/en-us/dotnet/api/system.stringsplitoptions?view=net-9.0>`_ enum)
        | Default value is ``StringSplitOptions.None``
    * - ``TrimChars``
      - Array of ``char``. Default value is ``[" "]`` aka whitespace.
    * - ``Metadata``
      - | Parsed as the ``Dictionary<string, string>`` object containing all global *metadata* which ``string`` values are parsed to a target type value by the :ref:`md-getmetadata-method`.

Configuration
-------------

By using the *metadata*, users can implement their own logic and extend the functionality of Ocelot e.g.

.. code-block:: json

  {
    "Routes": [
      {
        "UpstreamHttpMethod": [ "GET" ],
        "UpstreamPathTemplate": "/posts/{postId}",
        "DownstreamPathTemplate": "/api/posts/{postId}",
        "DownstreamHostAndPorts": [
          { "Host": "localhost", "Port": 80 }
        ],
        "Metadata": {
          "id": "FindPost",
          "tags": "tag1, tag2, area1, area2, func1",
          "plugin1.enabled": "true",
          "plugin1.values": "[1, 2, 3, 4, 5]",
          "plugin1.param": "value2",
          "plugin1.param2": "123",
          "plugin2/param1": "overwritten-value",
          "plugin2/data": "{\"name\":\"John Doe\",\"age\":30,\"city\":\"New York\",\"is_student\":false,\"hobbies\":[\"reading\",\"hiking\",\"cooking\"]}"
        }
      }
    ],
    "GlobalConfiguration": {
      "Metadata": {
        "instance_name": "machine-1",
        "plugin2/param1": "default-value"
      },
      "MetadataOptions": {
      }
    }
  }

Now, the route *metadata* can be accessed through the ``DownstreamRoute`` object:

.. code-block:: csharp
  :emphasize-lines: 20

  using Ocelot.Middleware;
  using Ocelot.Metadata;
  using Ocelot.Logging;

  public class MyMiddleware : OcelotMiddleware
  {
      private readonly RequestDelegate _next;
      private readonly IMyService _myService;

      public MyMiddleware(RequestDelegate next, IOcelotLoggerFactory loggerFactory, IMyService myService)
          : base(loggerFactory.CreateLogger<MyMiddleware>())
      {
          _next = next;
          _myService = myService;
      }

      public Task Invoke(HttpContext context)
      {
          Logger.LogDebug("My middleware started");
          var route = context.Items.DownstreamRoute();
          var id = route.GetMetadata<string>("id");
          var tags = route.GetMetadata<string[]>("tags");

          // Plugin 1 data
          var p1Enabled = route.GetMetadata<bool>("plugin1.enabled");
          var p1Values = route.GetMetadata<string[]>("plugin1.values");
          var p1Param = route.GetMetadata<string>("plugin1.param", "system-default-value");
          var p1Param2 = route.GetMetadata<int>("plugin1.param2");

          // Plugin 2 data
          var p2Param1 = route.GetMetadata<string>("plugin2/param1", "default-value");
          var json = route.GetMetadata<string>("plugin2/data");
          var plugin2 = System.Text.Json.JsonSerializer.Deserialize<Plugin2Data>(json);

          // Reading global metadata
          var globalInstanceName = route.GetMetadata<string>("instance_name");
          var globalPlugin2Param1 = route.GetMetadata<string>("plugin2/param1");

          // Working with plugin's metadata
          // ...
          return _next.Invoke(context);
      }
      public class Plugin2Data
      {
          public string name { get; set; }
          public int age { get; set; }
          public string city { get; set; }
          public bool is_student { get; set; }
          public string[] hobbies { get; set; }
      }
  }

.. _md-getmetadata-method:

``GetMetadata<T>`` Method
-------------------------

Ocelot provides one ``DowstreamRoute`` extension method to help you retrieve your *metadata* values effortlessly.
With the exception of the types ``string``, ``bool``, ``bool?``, ``string[]`` and numeric, all strings passed as parameters are treated as json strings and an attempt is made to convert them into objects of generic type T.
If the value is null, then, if not explicitely specified, the default for the chosen target type is returned.

.. list-table::
    :widths: 20 80
    :header-rows: 1

    * - *Method*
      - *Description*
    * - ``GetMetadata<string>``
      - The *metadata* value is returned as string without further parsing
    * - ``GetMetadata<string[]>``
      - | The *metadata* value is splitted by a given separator (default ``,``) and returned as a string array.
        | **Note**: Several parameters can be set in the global configuration, such as ``Separators`` (default = ``[","]``), ``StringSplitOptions`` (default ``None``) and ``TrimChars``, the characters that should be trimmed (default = ``[' ']``).
    * - ``GetMetadata<TInt>`` 
      - | The *metadata* value is parsed to a number. The ``TInt`` is any known numeric type, such as ``byte``, ``sbyte``, ``short``, ``ushort``, ``int``, ``uint``, ``long``, ``ulong``, ``float``, ``double``, ``decimal``.
        | **Note**: Some parameters can be set in the global configuration, such as ``NumberStyle`` (default ``Any``) and ``CurrentCulture`` (default ``CultureInfo.CurrentCulture``)
    * - ``GetMetadata<T>``
      - | The *metadata* value is converted to the given generic type. The value is treated as a json string and the json serializer tries to deserialize the string to the target type.
        | **Note**: A ``JsonSerializerOptions`` object can be passed as method parameter, ``Web`` is used as default.
    * - ``GetMetadata<bool>``
      - | Check if the *metadata* value is a truthy value, otherwise return ``false``.
        | **Note**: The truthy values are: ``true``, ``yes``, ``ok``, ``on``, ``enable``, ``enabled``
    * - ``GetMetadata<bool?>``
      - | Check if the *metadata* value is a truthy value (return ``true``), or falsy value (return ``false``), otherwise return ``null``.
        | **Note**: The known truthy values are: ``true``, ``yes``, ``ok``, ``on``, ``enable``, ``enabled``, ``1``, the known falsy values are: ``false``, ``no``, ``off``, ``disable``, ``disabled``, ``0``

Sample
------

The *Metadata* feature is a relatively new :doc:`../features/configuration` feature (anchored in the ":ref:`config-route-metadata`" section).

To introduce a standardized approach to middleware development, we have prepared a comprehensive sample project:

  | **Project**: `samples <https://github.com/ThreeMammals/Ocelot/tree/main/samples>`_ / `Metadata <https://github.com/ThreeMammals/Ocelot/tree/main/samples/Metadata>`_
  | **Solution**: `Ocelot.Samples.sln <https://github.com/ThreeMammals/Ocelot/blob/main/samples/Ocelot.Samples.sln>`_

The solution for the ``Ocelot.Samples.Metadata.csproj`` project includes the following capabilities:

- It has two custom Ocelot middlewares attached: ``PreErrorResponderMiddleware`` and ``ResponderMiddleware``.
  The ``PreErrorResponderMiddleware`` reads the route *metadata* based on the route ID and parses it.
  This is an example of how to parse or read the *metadata* of a specific route.
- The custom ``ResponderMiddleware`` simply calls the base Ocelot middleware (default implementation).
  Ocelot's ``ResponderMiddleware`` is responsible for writing the final body data into the ``HttpResponse`` of the current ``HttpContext``.
- The main `Program`_ replaces Ocelot's default ``IHttpResponder`` service with a custom ``MetadataResponder`` service.
  It attaches both ``PreErrorResponderMiddleware`` and ``ResponderMiddleware`` using the ``OcelotPipelineConfiguration`` argument in the ``UseOcelot`` method.
- The ``MetadataResponder`` service processes all JSON data when the ``Content-Type`` header has the value ``application/json``.
  This custom responder service writes the original data into the ``Response`` section and writes the route *metadata* back to the ``Metadata`` section using the following JSON schema:

    .. code-block:: json

      {
        "Response": {
          // Original data of the downstream response
        },
        "Metadata": {
          // current route metadata
        }
      }

- The ``MetadataResponder`` service always generates the custom ``OC-Route-Metadata`` header, containing the route *metadata* as a plain JSON string for all routes, regardless of the media type of the content.
  This allows you to parse it on the client side for specific purposes.
- The ``MetadataResponder`` service attempts to decompress the content body if it is compressed using one of the following algorithms from downstream endpoints: Brotli (``br``), GZip (``gzip``), or Zstandard (``zstd``).
  However, data compressed with the ``deflate`` algorithm is ignored and transferred to the client as-is because decompressing a third-party algorithm with a custom implementation is not feasible.
  Finally, the responder service returns uncompressed data and indicates this in the ``Content-Encoding`` header, where the value is always set to ``identity``.
- Processing JSON data can be disabled for specific routes using the ``disableMetadataJson`` option in the *metadata*.
  In this case, all JSON data is returned to the client as-is, preserving the original body streams (see the ``/ocelot/docs/`` route).

**Conclusion**: The purpose of this sample is to detect JSON data, process it, and embed a custom ``Metadata`` section while returning the original JSON data in the ``Response`` section.
This sample and its ``MetadataResponder`` service significantly increase response time due to on-the-fly JSON data processing, leading to degraded overall performance.
Please consider this as an example of processing *metadata*. For production environments, such processing should be disabled.
Instead, returning *metadata* in a custom header is likely the best solution if your client needs to know the currently executed route on Ocelot's side.

""""

.. [#f1] The *Metadata* feature was requested in issues `738`_ and `1990`_, and it was released as part of version `23.3`_.

.. _738: https://github.com/ThreeMammals/Ocelot/issues/738
.. _1990: https://github.com/ThreeMammals/Ocelot/issues/1990
.. _23.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Metadata/Program.cs
