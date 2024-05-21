Metadata
========

Configuration
-------------

Ocelot provides various features such as routing, authentication, caching, load
balancing, and more.
However, some users may encounter situations where Ocelot does not meet their
specific needs or they want to customize its behavior.
In such cases, Ocelot allows users to add metadata to the route configuration.
This property can store any arbitrary data that users can access in middlewares
or delegating handlers.

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
          "plugin2/param2": "{\"name\":\"John Doe\",\"age\":30,\"city\":\"New York\",\"is_student\":false,\"hobbies\":[\"reading\",\"hiking\",\"cooking\"]}"
        }
      }
    ],
    "GlobalConfiguration": {
      "Metadata": {
        "instance_name": "machine-1",
        "plugin2/param1": "default-value"
      }
    }
  }

Now, the route metadata can be accessed through the ``DownstreamRoute`` object:

.. code-block:: csharp

    public class MyMiddleware
    {
        public Task Invoke(HttpContext context, Func<Task> next)
        {
            var route = context.Items.DownstreamRoute();
            var enabled = route.GetMetadata<bool>("plugin1.enabled");
            var values = route.GetMetadata<string[]>("plugin1.values");
            var param1 = route.GetMetadata<string>("plugin1.param", "system-default-value");
            var param2 = route.GetMetadata<int>("plugin1.param2");

            // working on the plugin1's function

            return next?.Invoke();
        }
    }

Extension Methods
-----------------

Ocelot provides one DowstreamRoute extension method to help you retrieve your metadata values effortlessly.
With the exception of the types string, bool, bool?, string[] and numeric, all strings passed as parameters are treated as json strings and an attempt is made to convert them into objects of generic type T.
If the value is null, then, if not explicitely specified, the default for the chosen target type is returned.

.. list-table::
    :widths: 20 40 40

    * - Method
      - Description
      - Notes
    * - ``GetMetadata<string>``
      - The metadata value is returned as string without further parsing
      -  
    * - ``GetMetadata<string[]>``
      - The metadata value is splitted by a given separator (default ``,``) and returned as a string array.
      - Several parameters can be set in the global configuration, such as Separators (default = ``[","]``), StringSplitOptions (default ``None``) and TrimChars, the characters that should be trimmed (default = ``[' ']``).
    * - ``GetMetadata<Any known numeric type>``
      - The metadata value is parsed to a number.
      - Some parameters can be set in the global configuration, such as NumberStyle (default ``Any``) and CurrentCulture (default ``CultureInfo.CurrentCulture``)
    * - ``GetMetadata<T>``
      - The metadata value is converted to the given generic type. The value is treated as a json string and the json serializer tries to deserialize the string to the target type.
      - A JsonSerializerOptions object can be passed as method parameter, Web is used as default.
    * - ``GetMetadata<bool>``
      - Check if the metadata value is a truthy value, otherwise return false.
      - The truthy values are: ``true``, ``yes``, ``ok``, ``on``, ``enable``, ``enabled``
    * - ``GetMetadata<bool?>``
      - Check if the metadata value is a truthy value (return true), or falsy value (return false), otherwise return null.
      - The known truthy values are: ``true``, ``yes``, ``ok``, ``on``, ``enable``, ``enabled``, ``1``, the known falsy values are: ``false``, ``no``, ``off``, ``disable``, ``disabled``, ``0``
