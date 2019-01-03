namespace Ocelot.Cache.CacheManager.AcceptanceTests
{
    using System;
    using System.IO;

    public static class TestConfiguration
    {
        public static string ConfigurationPath => Path.Combine(AppContext.BaseDirectory, "ocelot.json");
    }
}
