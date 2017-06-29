namespace Ocelot.UnitTests.TestData
{
    using System.Collections.Generic;

    using Ocelot.Configuration.Builder;
    using Ocelot.Configuration.File;

    public class AuthenticationConfigTestData
    {
        public static IEnumerable<object[]> GetAuthenticationData()
        {
            yield return new object[] 
            {
                "IdentityServer",
                new IdentityServerConfigBuilder()
                    .WithRequireHttps(true)
                    .WithApiName("test")
                    .WithApiSecret("test")
                    .WithProviderRootUrl("test")
                    .Build(),
                new FileConfiguration
                {
                    ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            UpstreamPathTemplate = "/api/products/{productId}",
                            DownstreamPathTemplate = "/products/{productId}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            ReRouteIsCaseSensitive = true,
                            AuthenticationOptions = new FileAuthenticationOptions
                            {
                                AllowedScopes = new List<string>(),
                                Provider = "IdentityServer",
                                IdentityServerConfig = new FileIdentityServerConfig
                                {
                                    ProviderRootUrl = "http://localhost:51888",
                                    RequireHttps = false,
                                    ApiName = "api",
                                    ApiSecret = "secret"
                                }                                
                            },
                            AddHeadersToRequest =
                                {
                                    { "CustomerId", "Claims[CustomerId] > value" },
                                }
                        }
                    }
                }
            };

            yield return new object[]
            {
                "Jwt",
                new JwtConfigBuilder()
                    .WithAudience("a")
                    .WithAuthority("au")
                    .Build(),
                new FileConfiguration
                {
                    ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            UpstreamPathTemplate = "/api/products/{productId}",
                            DownstreamPathTemplate = "/products/{productId}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            ReRouteIsCaseSensitive = true,
                            AuthenticationOptions = new FileAuthenticationOptions
                            {
                                AllowedScopes = new List<string>(),
                                Provider = "IdentityServer",
                                JwtConfig = new FileJwtConfig
                                {
                                    Audience = "a",
                                    Authority = "au"
                                }
                            },
                            AddHeadersToRequest =
                            {
                                { "CustomerId", "Claims[CustomerId] > value" },
                            }
                        }
                    }
                }
            };
        }
    }
}
