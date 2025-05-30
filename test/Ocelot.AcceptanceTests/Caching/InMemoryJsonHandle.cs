﻿using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Configuration;
using CacheManager.Core.Utility;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Ocelot.AcceptanceTests.Caching;

public class InMemoryJsonHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
{
    private readonly ICacheSerializer _serializer;
    private readonly ConcurrentDictionary<string, Tuple<Type, byte[]>> _cache;

    public InMemoryJsonHandle(
        ICacheManagerConfiguration managerConfiguration,
        CacheHandleConfiguration configuration,
        ICacheSerializer serializer,
        ILoggerFactory loggerFactory) : base(managerConfiguration, configuration)
    {
        _cache = new ConcurrentDictionary<string, Tuple<Type, byte[]>>();
        _serializer = serializer;
        Logger = loggerFactory.CreateLogger<InMemoryJsonHandle<TCacheValue>>();
    }

    public override int Count => _cache.Count;

    protected override ILogger Logger { get; }

    public override void Clear() => _cache.Clear();

    public override void ClearRegion(string region)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(region, nameof(region));
        var key = string.Concat(region, ":");
        foreach (var item in _cache.Where(p => p.Key.StartsWith(key, StringComparison.OrdinalIgnoreCase)))
        {
            _cache.TryRemove(item.Key, out var val);
        }
    }

    public override bool Exists(string key)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        return _cache.ContainsKey(key);
    }

    public override bool Exists(string key, string region)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(region, nameof(region));
        var fullKey = GetKey(key, region);
        return _cache.ContainsKey(fullKey);
    }

    protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        var key = GetKey(item.Key, item.Region);
        var serializedItem = _serializer.SerializeCacheItem(item);
        return _cache.TryAdd(key, new Tuple<Type, byte[]>(item.Value.GetType(), serializedItem));
    }

    protected override CacheItem<TCacheValue> GetCacheItemInternal(string key) => GetCacheItemInternal(key, null);

    protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
    {
        var fullKey = GetKey(key, region);

        CacheItem<TCacheValue> deserializedResult = null;

        if (_cache.TryGetValue(fullKey, out var result))
        {
            deserializedResult = _serializer.DeserializeCacheItem<TCacheValue>(result.Item2, result.Item1);

            if (deserializedResult.ExpirationMode != ExpirationMode.None && IsExpired(deserializedResult, DateTime.UtcNow))
            {
                _cache.TryRemove(fullKey, out var removeResult);
                TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Expired, deserializedResult.Value);
                return null;
            }
        }

        return deserializedResult;
    }

    protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        var serializedItem = _serializer.SerializeCacheItem(item);
        _cache[GetKey(item.Key, item.Region)] = new Tuple<Type, byte[]>(item.Value.GetType(), serializedItem);
    }

    protected override bool RemoveInternal(string key) => RemoveInternal(key, null);

    protected override bool RemoveInternal(string key, string region)
    {
        var fullKey = GetKey(key, region);
        return _cache.TryRemove(fullKey, out var val);
    }

    private static string GetKey(string key, string region)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (string.IsNullOrWhiteSpace(region))
        {
            return key;
        }

        return string.Concat(region, ":", key);
    }

    private static bool IsExpired(CacheItem<TCacheValue> item, DateTime now)
    {
        if (item.ExpirationMode == ExpirationMode.Absolute
            && item.CreatedUtc.Add(item.ExpirationTimeout) < now)
        {
            return true;
        }
        else if (item.ExpirationMode == ExpirationMode.Sliding
            && item.LastAccessedUtc.Add(item.ExpirationTimeout) < now)
        {
            return true;
        }

        return false;
    }
}
