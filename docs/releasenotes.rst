.. _@raman-m: https://github.com/raman-m
.. _@ggnaegi: https://github.com/ggnaegi
.. _@EngRajabi: https://github.com/EngRajabi
.. _@jlukawska: https://github.com/jlukawska
.. _@kesskalli: https://github.com/kesskalli

.. _23.4: https://github.com/ThreeMammals/Ocelot/releases/tag/23.4.3
.. _23.4.0: https://github.com/ThreeMammals/Ocelot/releases/tag/23.4.0
.. _23.4.1: https://github.com/ThreeMammals/Ocelot/releases/tag/23.4.1
.. _23.4.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.4.2
.. _23.4.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.4.3

.. _1307: https://github.com/ThreeMammals/Ocelot/pull/1307
.. _1348: https://github.com/ThreeMammals/Ocelot/pull/1348
.. _1403: https://github.com/ThreeMammals/Ocelot/pull/1403
.. _2200: https://github.com/ThreeMammals/Ocelot/pull/2200
.. _2209: https://github.com/ThreeMammals/Ocelot/issues/2209
.. _2212: https://github.com/ThreeMammals/Ocelot/issues/2212
.. _2246: https://github.com/ThreeMammals/Ocelot/issues/2246

.. role::  htm(raw)
    :format: html
.. role:: pdf(raw)
   :format: latex pdflatex
.. _welcome:

#######
Welcome
#######

Welcome to the Ocelot `23.4`_ documentation!

It is recommended to read all :ref:`release-notes` if you have deployed the Ocelot app in a production environment and are planning to upgrade to major or patched versions.

.. _release-notes:

📢 Release Notes
-----------------

  | Release Tag: `23.4.0`_
  | Release Date: November 19, 2024
  | Release Codename: `McDonald's <https://www.youtube.com/watch?v=_PgYAPdOs9M>`_

  .. only:: html

    :htm:`<details><summary>Release Jokes:</summary>`

    - **for men**: Wearing a cap with the `MAGA slogan <https://www.bing.com/search?q=make+america+great+again+slogan>`_ is encouraged when visiting McDonald's.
    - **for women**: Donald is fond of caps, particularly the `MAGA cap <https://www.bing.com/search?q=make+america+great+again+cap>`_, and it's amusing to see children's reactions when `We Ask Kids How Mr.D is Doing <https://www.youtube.com/watch?v=XYviM5xevC8>`_?
    - **for black men**: Here are some highlights of Donald's antics aka Mr. D:

      1. `Mr. D stops to retrieve Marine's hat <https://www.youtube.com/watch?v=pAbgc41pksE>`_
      2. `M-A-G-A caps take flight <https://www.youtube.com/watch?v=jJDXj6-54wE>`_
      3. `Mr. D Dances To 'YMCA' <https://www.youtube.com/watch?v=Zph7YXfjMhg>`_
      4. `Elon is more than just a MAGAr <https://www.youtube.com/watch?v=zWSXmMiWTJ0&t=42s>`_
      5. `Mr. D looks for a job at McDonald's in 2024 <https://www.youtube.com/watch?v=_PgYAPdOs9M>`_
      6. lastly, `Mr. D serves customers at McDonald's Drive-Thru <https://www.youtube.com/watch?v=RwWDCh8O9WE>`_

    :htm:`</details>`

.. only:: latex

  .. admonition:: Release Jokes

    - **for men**: Wearing a cap with the `MAGA slogan <https://www.bing.com/search?q=make+america+great+again+slogan>`_ is encouraged when visiting McDonald's.
    - **for women**: Donald is fond of caps, particularly the `MAGA cap <https://www.bing.com/search?q=make+america+great+again+cap>`_, and it's amusing to see children's reactions when `We Ask Kids How Mr.D is Doing <https://www.youtube.com/watch?v=XYviM5xevC8>`_?
    - **for black men**: Here are some highlights of Donald's antics aka Mr. D:

      1. `Mr. D stops to retrieve Marine's hat <https://www.youtube.com/watch?v=pAbgc41pksE>`_
      2. `M-A-G-A caps take flight <https://www.youtube.com/watch?v=jJDXj6-54wE>`_
      3. `Mr. D Dances To 'YMCA' <https://www.youtube.com/watch?v=Zph7YXfjMhg>`_
      4. `Elon is more than just a MAGAr <https://www.youtube.com/watch?v=zWSXmMiWTJ0&t=42s>`_
      5. `Mr. D looks for a job at McDonald's in 2024 <https://www.youtube.com/watch?v=_PgYAPdOs9M>`_
      6. lastly, `Mr. D serves customers at McDonald's Drive-Thru <https://www.youtube.com/watch?v=RwWDCh8O9WE>`_

The major version `23.4.0`_ includes several patches, the history of which is provided below.

.. admonition:: Patches

  - `23.4.1`_, on Nov 22, 2024: Issues `2209`_, `2212`_ patch. Stabilize the routing :ref:`routing-placeholders` feature. 
  - `23.4.2`_, on Nov 27, 2024: End of .NET 6, 7 Support patch.
  - `23.4.3`_, on Jan 17, 2025: Issue `2246`_ patch. Redesign ``Regex`` caching to prevent the memory leak.

ℹ️ About
^^^^^^^^^

This minor release significantly upgrades the :doc:`../features/routing` feature by supporting :ref:`Embedded Placeholders <routing-embedded-placeholders>` within path segments (between slashes).
Additionally, the team has focused on enhancing the performance of ``Regex`` objects.

🆕 What's New?
^^^^^^^^^^^^^^^

- :doc:`../features/routing`: Introducing the new ":ref:`Embedded Placeholders <routing-embedded-placeholders>`" feature by `@ggnaegi`_ in pull request `2200`_.

  As of November 2024, Ocelot was unable to process multiple :ref:`routing-placeholders` embedded between two forward slashes (``/``).
  It was also challenging to differentiate the placeholder from other elements within the slashes.
  For example, ``/{url}-2/`` for ``/y-2/`` would yield ``{url} = y-2``.
  We are excited to introduce an enhanced method for evaluating placeholders that allows for the resolution of :ref:`routing-placeholders` within complex URLs.

🆙 What's Updated?
^^^^^^^^^^^^^^^^^^^

.. _Best Practices for Regular Expressions in .NET: https://docs.microsoft.com/en-us/dotnet/standard/base-types/best-practices
.. _RateLimitingHeaders: https://github.com/ThreeMammals/Ocelot/blob/23.4.0/src/Ocelot/RateLimiting/RateLimitingHeaders.cs
.. _Ocelot's RateLimiting headers do not align with industry standards: https://github.com/ThreeMammals/Ocelot/blob/27d3df2d0fdfbf5acde12d9442dfc08836e8b982/src/Ocelot/RateLimiting/RateLimitingHeaders.cs#L6
.. _ClaimsToHeadersMiddleware: https://ocelot.readthedocs.io/en/23.4/search.html?q=ClaimsToHeadersMiddleware

- `Core <https://github.com/ThreeMammals/Ocelot/labels/Core>`_: All ``Regex`` logic has been refactored by `@EngRajabi`_ in pull request `1348`_.

  The Ocelot Core now boasts improved performance of ``Regex`` objects, striving to adhere to the `Best Practices for Regular Expressions in .NET`_.
  It is estimated that each request could save from 1 to over 10 microseconds in processing time (though **no** benchmarks have been developed to measure this).

- :doc:`../features/ratelimiting`: The persistent issue with *Rate Limiting* headers has been resolved by `@jlukawska`_ in pull request `1307`_.

  The problem was the absence of unofficial ``X-Rate-Limit-*`` headers (found in the `RateLimitingHeaders`_ class) in the ``RateLimitingMiddleware``'s response.
  Note that these unofficial headers have not yet been documented, so they may be subject to change since `Ocelot's RateLimiting headers do not align with industry standards`_.

- :doc:`../features/middlewareinjection`: The ``ClaimsToHeadersMiddleware`` property has been introduced by `@kesskalli`_ in pull request `1403`_.

  This new property of the ``OcelotPipelineConfiguration`` class enables the overriding of the `ClaimsToHeadersMiddleware`_.

📘 Documentation
^^^^^^^^^^^^^^^^^

- :doc:`../features/routing`: New section on ":ref:`Embedded Placeholders <routing-embedded-placeholders>`".
- :doc:`../features/middlewareinjection`: Added the `ClaimsToHeadersMiddleware`_ property.

🧑‍💻 Contributing
------------------

.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :height: 30
  :target: https://github.com/ThreeMammals/Ocelot/
.. _Pull requests: https://github.com/ThreeMammals/Ocelot/pulls
.. _issues: https://github.com/ThreeMammals/Ocelot/issues
.. _Ocelot GitHub: https://github.com/ThreeMammals/Ocelot/
.. _Ocelot Discussions: https://github.com/ThreeMammals/Ocelot/discussions
.. _ideas: https://github.com/ThreeMammals/Ocelot/discussions/categories/ideas
.. _questions: https://github.com/ThreeMammals/Ocelot/discussions/categories/q-a

`Pull requests`_, `issues`_, and commentary are welcome at the `Ocelot GitHub`_ repository.

For `ideas`_ and `questions`_, please post them in the `Ocelot Discussions`_ space.

Our :doc:`../building/devprocess` is a part of successful :doc:`../building/releaseprocess`.
If you are a new contributor, it is crucial to read :doc:`../building/devprocess` attentively to grasp our methods for efficient and swift feature delivery.
We, as a team, advocate adhering to :ref:`dev-best-practices` throughout the development phase.

We extend our best wishes for your successful contributions to the Ocelot product!
