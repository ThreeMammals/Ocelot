using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ocelot.Configuration.File
{

    public class FileRateLimitRule
    {
        public FileRateLimitRule()
        {
            ClientWhitelist = new List<string>();
        }

        public List<string> ClientWhitelist { get; set; }

        /// <summary>
        /// Enables endpoint rate limiting based URL path and HTTP verb
        /// </summary>
        public bool EnableRateLimiting { get; set; }

        /// <summary>
        /// Rate limit period as in 1s, 1m, 1h
        /// </summary>
        public string Period { get; set; }

        public double PeriodTimespan { get; set; }
        /// <summary>
        /// Maximum number of requests that a client can make in a defined period
        /// </summary>
        public long Limit { get; set; }
    }
}
