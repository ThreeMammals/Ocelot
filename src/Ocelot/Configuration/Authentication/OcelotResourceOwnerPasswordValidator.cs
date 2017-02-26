using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Ocelot.Configuration.Provider;

namespace Ocelot.Configuration.Authentication
{
    public class OcelotResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly IHashMatcher _matcher;
        private readonly IIdentityServerConfiguration _identityServerConfiguration;

        public OcelotResourceOwnerPasswordValidator(IHashMatcher matcher, IIdentityServerConfiguration identityServerConfiguration)
        {
            _identityServerConfiguration = identityServerConfiguration;
            _matcher = matcher;
        }

        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            try
            {
                var user = _identityServerConfiguration.Users.FirstOrDefault(u => u.UserName == context.UserName);

                if(user == null)
                {
                    context.Result = new GrantValidationResult(
                            TokenRequestErrors.InvalidGrant,
                            "invalid custom credential");
                }
                else if(_matcher.Match(context.Password, user.Salt, user.Hash))
                {
                    context.Result = new GrantValidationResult(
                        subject: "admin",
                        authenticationMethod: "custom");
                }
                else
                {
                    context.Result = new GrantValidationResult(
                        TokenRequestErrors.InvalidGrant,
                        "invalid custom credential");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
    }
}