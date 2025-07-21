.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/ocelot.json
.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/Program.cs

Headers Transformation
======================

Ocelot allows the user to transform `HTTP headers <https://developer.mozilla.org/en-US/docs/Glossary/HTTP_header>`_ both before and after the downstream request.

.. _ht-find-and-replace:

Find and Replace [#f1]_
-----------------------

In order to transform a header first we specify the header key and then the type of transform we want e.g.

.. code-block:: json

  "Test": "http://www.bbc.co.uk/, http://ocelot.com/"

The key is ``Test`` and the value is ``http://www.bbc.co.uk/, http://ocelot.com/``.
The value is saying: replace ``http://www.bbc.co.uk/`` with ``http://ocelot.com/``.
The syntax is ``{find}, {replace}``. Hopefully pretty simple. There are examples below that explain more.

Pre Downstream Request
^^^^^^^^^^^^^^^^^^^^^^

Add the following to a Route in `ocelot.json`_ in order to replace ``http://www.bbc.co.uk/`` with ``http://ocelot.com/``.
This header will be changed before the request downstream and will be sent to the downstream server.

.. code-block:: json

  "UpstreamHeaderTransform": {
    "Test": "http://www.bbc.co.uk/, http://ocelot.com/"
  }

Post Downstream Request
^^^^^^^^^^^^^^^^^^^^^^^

Add the following to a Route in `ocelot.json`_ in order to replace ``http://www.bbc.co.uk/`` with ``http://ocelot.com/``.
This transformation will take place after Ocelot has received the response from the downstream service.

.. code-block:: json

  "DownstreamHeaderTransform": {
    "Test": "http://www.bbc.co.uk/, http://ocelot.com/"
  }

.. _ht-add-to-request:

Add to Request [#f2]_
---------------------

If you want to add a header to your upstream request please add the following to a route in your `ocelot.json`_:

.. code-block:: json

  "UpstreamHeaderTransform": {
    "Uncle": "Bob"
  }

In the example above a header with the key ``Uncle`` and value ``Bob`` would be send to to the upstream service.

:ref:`ht-placeholders` are supported too (see below).

.. _ht-add-to-response:

Add to Response [#f3]_
----------------------

If you want to add a header to your downstream response, please add the following to a route in `ocelot.json`_:

.. code-block:: json

  "DownstreamHeaderTransform": {
    "Uncle": "Bob"
  }

In the example above a header with the key ``Uncle`` and value ``Bob`` would be returned by Ocelot when requesting the specific route.

If you want to return the :ref:`tr-butterfly` Trace ID, do something like the following:

.. code-block:: json

  "DownstreamHeaderTransform": {
    "AnyKey": "{TraceId}"
  }

.. _ht-placeholders:

Placeholders
------------

Ocelot allows placeholders that can be used in header transformation.

.. list-table::
  :widths: 25 75
  :header-rows: 1

  * - *Placeholder*
    - *Description*
  * - ``{BaseUrl}``
    - This will use Ocelot base URL e.g. ``http://localhost:5000`` as its value.
  * - ``{DownstreamBaseUrl}``
    - This will use the downstream services base URL e.g. ``http://localhost:5000`` as its value. This only works for ``DownstreamHeaderTransform`` route option at the moment.
  * - ``{RemoteIpAddress}``
    - This will find the clients IP address using ``HttpContext.Connection.RemoteIpAddress``, so you will get back some IP. See more in the `GetRemoteIpAddress <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20%22Response%3Cstring%3E%20GetRemoteIpAddress()%22&type=code>`_ method.
  * - ``{TraceId}``
    - This will use the :ref:`tr-butterfly` Trace ID. This only works for ``DownstreamHeaderTransform`` route option at the moment.
  * - ``{UpstreamHost}``
    - This will look for the incoming ``Host`` header.

For now, we believe these placeholders are sufficient for basic user scenarios.
However, if you need more placeholders, you can head to the :ref:`ht-future`.

Samples
-------

Handling 302 redirects
^^^^^^^^^^^^^^^^^^^^^^

Ocelot will by default automatically follow redirects, however if you want to return the location header to the client, you might want to change the location to be Ocelot not the downstream service.
Ocelot allows this with the following configuration:

.. code-block:: json

  "DownstreamHeaderTransform": {
    "Location": "http://www.bbc.co.uk/, http://ocelot.com/"
  },
  "HttpHandlerOptions": {
    "AllowAutoRedirect": false,
  }

Or, you could use the ``{BaseUrl}`` placeholder.

.. code-block:: json

  "DownstreamHeaderTransform": {
    "Location": "http://localhost:6773, {BaseUrl}"
  },
  "HttpHandlerOptions": {
    "AllowAutoRedirect": false,
  }

Finally, if you are using a load balancer with Ocelot, you will get multiple downstream base URLs so the above would not work.
In this case you can do the following:

.. code-block:: json

  "DownstreamHeaderTransform": {
    "Location": "{DownstreamBaseUrl}, {BaseUrl}"
  },
  "HttpHandlerOptions": {
    "AllowAutoRedirect": false,
  }

``X-Forwarded-For`` header
^^^^^^^^^^^^^^^^^^^^^^^^^^

An example of using ``{RemoteIpAddress}`` placeholder:

.. code-block:: json

  "UpstreamHeaderTransform": {
    "X-Forwarded-For": "{RemoteIpAddress}"
  }

.. _ht-future:

Future
------

Ideally this feature would be able to support the fact that a header can have multiple values.
At the moment it just assumes one.
It would also be nice if it could multi find and replace e.g. 

.. code-block:: json

  "DownstreamHeaderTransform": {
    "Location": "[{one,one},{two,two}]"
  },
  "HttpHandlerOptions": {
    "AllowAutoRedirect": false,
  }

If anyone wants to have a go at this please, help yourself!

Global Transformation
^^^^^^^^^^^^^^^^^^^^^

.. _1658: https://github.com/ThreeMammals/Ocelot/issues/1658
.. _1659: https://github.com/ThreeMammals/Ocelot/pull/1659
.. _20.0: https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0

We have a pending open pull request `1659`_ for issue `1658`_.
Versions `20.0`_ and higher provide route-level *Headers Transformation* features,
but we hope *global* transformations will be included in the next upcoming `release <https://github.com/ThreeMammals/Ocelot/releases>`_.

.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :height: 25
  :class: img-valign-middle

Any ideas and proposals can be shared in the `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ space of the repository! |octocat|

""""

.. [#f1] The ":ref:`ht-find-and-replace`" feature was requested in issue `190`_, initially released in version `2.0.11`_, and the team decided that it would be useful in various ways.
.. [#f2] The ":ref:`ht-add-to-request`" feature was requested in issue `313`_ and released in version `5.5.3`_.
.. [#f3] The ":ref:`ht-add-to-response`" feature was requested in issue `280`_ and released in version `5.1.0`_.

.. _2.0.11: https://github.com/ThreeMammals/Ocelot/releases/tag/2.0.11
.. _5.1.0: https://github.com/ThreeMammals/Ocelot/releases/tag/5.1.0
.. _5.5.3: https://github.com/ThreeMammals/Ocelot/releases/tag/5.5.3
.. _190: https://github.com/ThreeMammals/Ocelot/issues/190
.. _280: https://github.com/ThreeMammals/Ocelot/issues/280
.. _313: https://github.com/ThreeMammals/Ocelot/issues/313
