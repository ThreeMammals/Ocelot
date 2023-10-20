Big Picture
===========

Ocelot is aimed at people using .NET running a microservices / service-oriented architecture 
that need a unified point of entry into their system. However it will work with anything that speaks HTTP(S) and run on any platform that ASP.NET Core supports.

In particular we want easy integration with `IdentityServer <https://github.com/IdentityServer>`_ reference and `Bearer <https://oauth.net/2/bearer-tokens/>`_ tokens. 
We have been unable to find this in our current workplace without having to write our own Javascript middlewares to handle the IdentityServer reference tokens.
We would rather use the IdentityServer code that already exists to do this.

Ocelot is a bunch of middlewares in a specific order.

Ocelot manipulates the ``HttpRequest`` object into a state specified by its configuration until it reaches a request builder middleware,
where it creates a ``HttpRequestMessage`` object which is used to make a request to a downstream service.
The middleware that makes the request is the last thing in the Ocelot pipeline. It does not call the next middleware.
The response from the downstream service is retrieved as the requests goes back up the Ocelot pipeline.
There is a piece of middleware that maps the ``HttpResponseMessage`` onto the ``HttpResponse`` object and that is returned to the client.
That is basically it with a bunch of other features!

The following are configurations that you use when deploying Ocelot.

Basic Implementation
^^^^^^^^^^^^^^^^^^^^
.. image:: ../images/OcelotBasic.jpg

With IdentityServer
^^^^^^^^^^^^^^^^^^^
.. image:: ../images/OcelotIndentityServer.jpg

Multiple Instances
^^^^^^^^^^^^^^^^^^
.. image:: ../images/OcelotMultipleInstances.jpg

With Consul
^^^^^^^^^^^
.. image:: ../images/OcelotMultipleInstancesConsul.jpg

With Service Fabric
^^^^^^^^^^^^^^^^^^^
.. image:: ../images/OcelotServiceFabric.jpg
