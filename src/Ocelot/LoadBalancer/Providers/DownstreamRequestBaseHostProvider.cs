namespace Ocelot.LoadBalancer.Providers
{
    public class DownstreamRequestBaseHostProvider : IDownstreamRequestBaseHostProvider
    {
        private const string DoubleSlashes = "://";

        /// <summary>
        /// Gets the base host information.
        /// </summary>
        /// <param name="downstreamHost">The downstream host.</param>
        /// <remarks>Parses out base host address and application name from host address with sub apllication name, e.g. 'hostaddress/app' 
        /// and returns 'hostaddress' as base host address and '/app' as an application name to be later used for proper downstream request creation.</remarks>
        /// <returns><see cref="BaseHostInfo"/></returns>
        public BaseHostInfo GetBaseHostInfo(string downstreamHost)
        {
            var doubleSlashesIndex = downstreamHost.IndexOf(DoubleSlashes);
            var start = (doubleSlashesIndex != -1) ? doubleSlashesIndex + DoubleSlashes.Length : 0;
            var end = downstreamHost.IndexOf("/", start);
            if (end == -1)
            {
                end = downstreamHost.Length;
            }

            var baseHost = downstreamHost.Substring(start, end - start);

            var portIndex = baseHost.IndexOf(":");
            if (portIndex != -1)
            {
                var portEnd = (portIndex != -1) ? portIndex : baseHost.Length;
                baseHost = baseHost.Substring(0, portEnd);
            }

            var appName = downstreamHost.Substring(end, downstreamHost.Length - end);

            return new BaseHostInfo()
            {
                BaseHost = baseHost,
                ApplicationName = appName
            };
        }
    }
}
