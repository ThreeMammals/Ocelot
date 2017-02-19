using System;

namespace Ocelot.AcceptanceTests
{
    public static class TestConfiguration
    {
        public static string ConfigurationPath => $"{AppContext.BaseDirectory}/configuration.json";
    }
}
