# Ocelot

Attempt at a .NET Api Gateway

This project is aimed at people using .NET running 
a micro services / service orientated architecture 
that need a unified point of entry into their system.

In particular I want easy integration with 
IdentityServer reference and bearer tokens. 

We have been unable to find this in my current workplace
without having to write our own Javascript middlewares 
to handle the IdentityServer reference tokens. We would
rather use the IdentityServer code that already exists
to do this.

Priorities

- Route configuration
- IdentityServer reference & bearer tokens
- Strip claims from tokens and use in proxy request
- Authorise access to routes based on claims in token
- Output Caching
- Monitoring
- Logging
- Rate Limiting
- Then a big list of cool things...

## How to use
