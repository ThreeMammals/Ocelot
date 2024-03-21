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
            var enabled = metadata.IsMetadataValueTruthy("plugin1.enabled");
            var values = metadata.GetMetadataValues("plugin1.values");
            var param1 = metadata.GetMetadataValue("plugin1.param", "system-default-value");
            var param2 = metadata.GetMetadataNumber<int>("plugin1.param2");

            // working on the plugin1's function

            return next?.Invoke();
        }
    }

Extension Methods
-----------------

Ocelot provides some extension methods help you to retrieve your metadata values effortlessly.

.. list-table::
    :widths: 20 40 40

    * - Method
      - Description
      - Notes
    * - ``GetMetadataValue``
      - The metadata value is a string.
      -
    * - ``GetMetadataValues``
      - The metadata value is spitted by a given separator (default ``,``) and 
        returned as a string array.
      -
    * - ``GetMetadataNumber<T>``
      - The metadata value is parsed to a number.
      - | Only available in .NET 7 or above.
        | For .NET 6, use ``GetMetadataFromJson<>``.
    * - ``GetMetadataFromJson<T>``
      - The metadata value is serialized to the given generic type.
      -
    * - ``IsMetadataValueTruthy``
      - Check if the metadata value is a truthy value.
      - The truthy values are: ``true``, ``yes``, ``ok``, ``on``, ``enable``, ``enabled``
