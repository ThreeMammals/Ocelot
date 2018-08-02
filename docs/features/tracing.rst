Tracing
=======

This page details how to perform distributed tracing with Ocelot. At the moment we only support Butterfly but other tracers might just work without
anything Ocelot specific.

Butterfly
^^^^^^^^^

Ocelot providers tracing functionality from the excellent `Butterfly <https://github.com/liuhaoyang/butterfly>`_ project. The code for the Ocelot integration
can be found `here <https://github.com/ThreeMammals/Ocelot.Tracing.Butterfly>`_.

In order to use the tracing please read the Butterfly documentation.

In ocelot you need to do the following if you wish to trace a ReRoute.

   ``Install-Package Ocelot.Tracing.Butterfly``

In your ConfigureServices method

.. code-block:: csharp

    services
        .AddOcelot()
        // this comes from Ocelot.Tracing.Butterfly package
        .AddButterfly(option =>
        {
            //this is the url that the butterfly collector server is running on...
            option.CollectorUrl = "http://localhost:9618";
            option.Service = "Ocelot";
        });

Then in your ocelot.json add the following to the ReRoute you want to trace..

.. code-block:: json

      "HttpHandlerOptions": {
            "UseTracing": true
        },

Ocelot will now send tracing information to Butterfly when this ReRoute is called.
