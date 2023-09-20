﻿using System;

namespace Ocelot.Cache
{
    internal class CacheObject<T>
    {
        public CacheObject(T value, DateTime expires)
        {
            Value = value;
            Expires = expires;
        }

        public T Value { get; }
        public DateTime Expires { get; }
    }
}
