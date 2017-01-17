namespace Ocelot.AcceptanceTests
{
    using System.Runtime.InteropServices;

    public static class TestConfiguration
    {
        public static double Version => 1.4;
        //public static string Runtime => "win10-x64";
        public static string ConfigurationPath => GetConfigurationPath();

        public static string GetConfigurationPath()
        {
            var osArchitecture = RuntimeInformation.OSArchitecture.ToString();
            
            var oSDescription = string.Empty;

            if(RuntimeInformation.OSDescription.ToLower().Contains("darwin"))
            {
                oSDescription = "osx.10.11";
            }

            if(RuntimeInformation.OSDescription.ToLower().Contains("windows"))
            {
                oSDescription = "win10";
            }

            var runTime = $"{oSDescription}-{osArchitecture}".ToLower();

            var configPath = $"./bin/Debug/netcoreapp{Version}/{runTime}/configuration.json";

            return configPath;
        }
    }
}
