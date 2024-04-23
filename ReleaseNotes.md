## Hotfix release (version {0}) for #2031 issue
> Route path template placeholders and their validation rules

Special thanks to **[Guillaume Gnaegi](https://github.com/ggnaegi)** and [Fabrizio Mancin](https://github.com/Fabman08)!

### About
The bug is related to the [Placeholders](https://ocelot.readthedocs.io/en/latest/features/routing.html#placeholders) feature in [Configuration](https://ocelot.readthedocs.io/en/latest/features/configuration.html) and [Routing](https://ocelot.readthedocs.io/en/latest/features/routing.html).
The bug was introduced in version [23.2.0](https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0) as a part of PR #1927.

### Breaking Change
The new [validation rules](https://github.com/ThreeMammals/Ocelot/blob/23.2.0/src/Ocelot/Configuration/Validator/FileConfigurationFluentValidator.cs#L45-L50) of the `FileConfigurationFluentValidator` class do not allow the Ocelot app to start when implicit [placeholders](https://ocelot.readthedocs.io/en/latest/features/routing.html#placeholders) are defined in custom implementations, such as middlewares, delegating handlers, and replaced services in the dependency injection (DI) container.
These new rules are capable of validating explicit [placeholders](https://ocelot.readthedocs.io/en/latest/features/routing.html#placeholders) only within the `UpstreamPathTemplate` and `DownstreamPathTemplate` properties. Unfortunately, they cannot oversee implicit placeholders in custom implementations, and they do not validate early during the Ocelot app startup process.

Ensure that you avoid using version [23.2.0](https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0). If you are currently on that version, upgrade to version [{0}](https://github.com/ThreeMammals/Ocelot/releases/tag/{0}) by applying this hotfix patch.

### Technical info
With version [23.2.0](https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0), particularly if you have overridden certain service classes or implemented custom logic that manipulates placeholders, you may encounter Ocelot app crashes accompanied by the following errors in the log:
```
One or more errors occurred. (Unable to start Ocelot, errors are: XXX)
```
where `XXX` are the following validation error messages:
- `UpstreamPathTemplate 'UUU' doesn't contain the same placeholders in DownstreamPathTemplate 'DDD'`
- `DownstreamPathTemplate 'DDD' doesn't contain the same placeholders in UpstreamPathTemplate 'UUU'`

**Finally**, the [validation rules](https://github.com/ThreeMammals/Ocelot/blob/23.2.0/src/Ocelot/Configuration/Validator/FileConfigurationFluentValidator.cs#L45-L50) resulted from the incorrect assumption that placeholders are always explicit and can be validated early. Therefore, custom implementations and feature services in the dependency injection (DI) container, which rely on or manipulate placeholders, should validate the configuration JSON and appropriate options later, directly within their service implementations.

### Bug Artifacts
- Released in version: [23.2.0](https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0)
- Introduced in: PR #1927
- Reported bug: #2031 by @ggnaegi and tested by @Fabman08
- Hotfix PR: #2032 by @raman-m
