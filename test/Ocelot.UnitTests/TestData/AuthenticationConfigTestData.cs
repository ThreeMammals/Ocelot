namespace Ocelot.UnitTests.TestData
{
    using System.Collections.Generic;
    using Ocelot.Configuration.File;

    public class AuthenticationConfigTestData
    {
        public static IEnumerable<object[]> GetAuthenticationData()
        {
            yield return new object[] 
            {
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
                                AuthenticationProviderKey = "Test",
                                AllowedScopes = new List<string>(),
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
                                AuthenticationProviderKey = "Test",
                                AllowedScopes = new List<string>(),
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
