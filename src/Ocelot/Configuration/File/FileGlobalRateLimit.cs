﻿namespace Ocelot.Configuration.File
{
    public sealed record FileGlobalRateLimit
    {
        public string Name { get; init; }
        public string Pattern { get; init; } 
        public List<string> Methods { get; init; }
        public int Limit { get; init; }
        public string Period { get; init; } 
        public int HttpStatusCode { get; init; }= 429;
        public string QuotaExceededMessage { get; init; }
        public bool DisableRateLimitHeaders { get; init; }
        public bool EnableRateLimiting { get; init; }
    }
}
