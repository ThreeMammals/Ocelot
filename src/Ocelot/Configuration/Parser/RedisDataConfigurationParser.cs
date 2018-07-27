using System;
using System.Collections.Generic;
using System.Linq;
using Ocelot.Configuration.File;
using Ocelot.DynamicConfigurationProvider;
using StackExchange.Redis;

namespace Ocelot.Configuration.Parser
{
    public class RedisDataConfigurationParser : IRedisDataConfigurationParser
    {
        public FileReRoute Parse(List<HashEntry> hashEntries)
        {
            if (hashEntries == null || hashEntries.Count() == 0)
            {
                return new FileReRoute();
            }

            var hashesDict = hashEntries.ToDictionary(x => x.Name, x => x.Value);

            var fileReRoute = new FileReRoute()
            {
                RateLimitOptions = new FileRateLimitRule
                {
                    ClientWhitelist = ParseString(GetRedisValue(hashesDict, ConfigurationKeys.RateLimit.CLIENT_WHITE_LIST))?.Split(',')?.Select(x => x.Trim())?.ToList(),
                    EnableRateLimiting = ParseBoolean(GetRedisValue(hashesDict, ConfigurationKeys.RateLimit.ENABLE_RATE_LIMITING), false),
                    Limit = ParseInt(GetRedisValue(hashesDict, ConfigurationKeys.RateLimit.LIMIT)),
                    Period = ParseString(GetRedisValue(hashesDict, ConfigurationKeys.RateLimit.PERIOD)),
                    PeriodTimespan = ParseDouble(GetRedisValue(hashesDict, ConfigurationKeys.RateLimit.PERIOD_TIME_SPAN))
                }
            };

            return fileReRoute;
        }

        private string ParseString(RedisValue redisValue, string defaultValue = null)
        {
            if (!redisValue.IsNullOrEmpty)
            {
                return redisValue.ToString();
            }

            return defaultValue;
        }

        private int ParseInt(RedisValue redisValue, int defaultValue = 0)
        {
            int result = 0;
            if (!redisValue.IsNullOrEmpty && int.TryParse(redisValue.ToString(), out result))
            {
                return result;
            }

            return defaultValue;
        }

        private double ParseDouble(RedisValue redisValue, double defaultValue = 0)
        {
            double result = 0;
            if (!redisValue.IsNullOrEmpty && double.TryParse(redisValue.ToString(), out result))
            {
                return result;
            }

            return defaultValue;
        }

        private bool ParseBoolean(RedisValue redisValue, bool defaultValue = true)
        {
            var result = defaultValue;
            if (!redisValue.IsNullOrEmpty)
            {
                if (redisValue.IsInteger)
                {
                    return redisValue.ToString().Equals("1");
                }
                else if(bool.TryParse(redisValue.ToString(), out result))
                {
                    return result;
                }
            }

            return defaultValue;
        }

        private RedisValue GetRedisValue(Dictionary<RedisValue, RedisValue> hashDictonary, string key)
        {
            if (hashDictonary.ContainsKey(key))
            {
                return hashDictonary[key];
            }

            return RedisValue.Null;
        }
    }
}
