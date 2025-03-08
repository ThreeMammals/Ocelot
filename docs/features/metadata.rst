Metadata
========

  Feature of: :doc:`../features/configuration`

Ocelot provides various features such as routing, authentication, caching, load balancing, and more.
However, some users may encounter situations where Ocelot does not meet their specific needs or they want to customize its behavior.
In such cases, Ocelot allows users to add metadata to the route configuration.
This property can store any arbitrary data that users can access in middlewares or delegating handlers.

Schema
------

As you may already know from the :doc:`../features/configuration` chapter and the :ref:`config-route-metadata` section, the route metadata schema is quite simple which is JSON dictionary:

.. code-block:: json

  "Metadata": {
    // "key": "value",
  }

.. _FileMetadataOptions: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileMetadataOptions.cs

  Class: `FileMetadataOptions`_

But there is **global** metadata configuration: the ``MetadataOptions`` *schema*.
You do not need to set all of these things, but this is everything that is available at the moment.

.. code-block:: json

  "MetadataOptions": {
    "CurrentCulture": "en-GB",
    "NumberStyle": "Any",
    "Separators": [","],
    "StringSplitOption": "None",
    "TrimChars": [" "],
    "Metadata": {} // dictionary
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
      - | Parsed as the ``Dictionary<string, string>`` object containing all global metadata which ``string`` values are parsed to a target type value by the :ref:`md-getmetadata-method`.

Configuration
-------------

By using the metadata, users can implement their own logic and extend the
functionality of Ocelot e.g.

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
      "MetadataOptions": {
        "Metadata": {
          "instance_name": "machine-1",
          "plugin2/param1": "default-value"
        }
      }
    }
  }

Now, the route metadata can be accessed through the ``DownstreamRoute`` object:

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

Ocelot provides one ``DowstreamRoute`` extension method to help you retrieve your metadata values effortlessly.
With the exception of the types ``string``, ``bool``, ``bool?``, ``string[]`` and numeric, all strings passed as parameters are treated as json strings and an attempt is made to convert them into objects of generic type T.
If the value is null, then, if not explicitely specified, the default for the chosen target type is returned.

.. list-table::
    :widths: 20 80
    :header-rows: 1

    * - *Method*
      - *Description*
    * - ``GetMetadata<string>``
      - The metadata value is returned as string without further parsing
    * - ``GetMetadata<string[]>``
      - | The metadata value is splitted by a given separator (default ``,``) and returned as a string array.
        | **Note**: Several parameters can be set in the global configuration, such as ``Separators`` (default = ``[","]``), ``StringSplitOptions`` (default ``None``) and ``TrimChars``, the characters that should be trimmed (default = ``[' ']``).
    * - ``GetMetadata<TInt>`` 
      - | The metadata value is parsed to a number. The ``TInt`` is any known numeric type, such as ``byte``, ``sbyte``, ``short``, ``ushort``, ``int``, ``uint``, ``long``, ``ulong``, ``float``, ``double``, ``decimal``.
        | **Note**: Some parameters can be set in the global configuration, such as ``NumberStyle`` (default ``Any``) and ``CurrentCulture`` (default ``CultureInfo.CurrentCulture``)
    * - ``GetMetadata<T>``
      - | The metadata value is converted to the given generic type. The value is treated as a json string and the json serializer tries to deserialize the string to the target type.
        | **Note**: A ``JsonSerializerOptions`` object can be passed as method parameter, ``Web`` is used as default.
    * - ``GetMetadata<bool>``
      - | Check if the metadata value is a truthy value, otherwise return ``false``.
        | **Note**: The truthy values are: ``true``, ``yes``, ``ok``, ``on``, ``enable``, ``enabled``
    * - ``GetMetadata<bool?>``
      - | Check if the metadata value is a truthy value (return ``true``), or falsy value (return ``false``), otherwise return ``null``.
        | **Note**: The known truthy values are: ``true``, ``yes``, ``ok``, ``on``, ``enable``, ``enabled``, ``1``, the known falsy values are: ``false``, ``no``, ``off``, ``disable``, ``disabled``, ``0``

Sample
------

To be written...
