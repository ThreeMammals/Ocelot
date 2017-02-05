namespace Ocelot.AcceptanceTests
{
    using System.Runtime.InteropServices;

    public static class TestConfiguration
    {
        public static double Version => 1.1;
        public static string ConfigurationPath => GetConfigurationPath();

        public static string GetConfigurationPath()
        {
            var osArchitecture = RuntimeInformation.OSArchitecture.ToString();

            if(RuntimeInformation.OSDescription.ToLower().Contains("darwin"))
            {
                return FormatConfigurationPath("osx.10.11", osArchitecture);
            }

            if(RuntimeInformation.OSDescription.ToLower().Contains("microsoft windows 10"))
            {                
                return FormatConfigurationPath("win10", osArchitecture);
            }
            
            return FormatConfigurationPath("win7", osArchitecture);
        }

        private static string FormatConfigurationPath(string oSDescription, string osArchitecture)
        {
            var runTime = $"{oSDescription}-{osArchitecture}".ToLower();

            var configPath = $"./bin/Debug/netcoreapp{Version}/{runTime}/configuration.json";

            return configPath;
        }
    }
}
