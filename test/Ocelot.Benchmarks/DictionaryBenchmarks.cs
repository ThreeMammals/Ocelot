using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Validators;
using System.Net.Http;

namespace Ocelot.Benchmarks
{
    using Configuration;
    using Configuration.Builder;
    using Requester;
    using System.Collections.Concurrent;

    [Config(typeof(DictionaryBenchmarks))]
    public class DictionaryBenchmarks : ManualConfig
    {
        private ConcurrentDictionary<DownstreamReRoute, IHttpClient> _downstreamReRouteDictionary;
        private ConcurrentDictionary<string, IHttpClient> _stringReRouteDictionary;
        private HttpClientWrapper _client;
        private string _stringKey;
        private DownstreamReRoute _downstreamReRouteKey;

        public DictionaryBenchmarks()
        {
            Add(StatisticColumn.AllStatistics);
            Add(MemoryDiagnoser.Default);
            Add(BaselineValidator.FailOnError);
        }

        [GlobalSetup]
        public void SetUp()
        {
            _downstreamReRouteKey = new DownstreamReRouteBuilder().Build();
            _stringKey = "test";
            _client = new HttpClientWrapper(new HttpClient());
            _downstreamReRouteDictionary = new ConcurrentDictionary<DownstreamReRoute, IHttpClient>();

            _downstreamReRouteDictionary.TryAdd(new DownstreamReRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamReRouteDictionary.TryAdd(new DownstreamReRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamReRouteDictionary.TryAdd(new DownstreamReRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamReRouteDictionary.TryAdd(new DownstreamReRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamReRouteDictionary.TryAdd(new DownstreamReRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamReRouteDictionary.TryAdd(new DownstreamReRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamReRouteDictionary.TryAdd(new DownstreamReRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamReRouteDictionary.TryAdd(new DownstreamReRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamReRouteDictionary.TryAdd(new DownstreamReRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamReRouteDictionary.TryAdd(new DownstreamReRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));

            _stringReRouteDictionary = new ConcurrentDictionary<string, IHttpClient>();
            _stringReRouteDictionary.TryAdd("1", new HttpClientWrapper(new HttpClient()));
            _stringReRouteDictionary.TryAdd("2", new HttpClientWrapper(new HttpClient()));
            _stringReRouteDictionary.TryAdd("3", new HttpClientWrapper(new HttpClient()));
            _stringReRouteDictionary.TryAdd("4", new HttpClientWrapper(new HttpClient()));
            _stringReRouteDictionary.TryAdd("5", new HttpClientWrapper(new HttpClient()));
            _stringReRouteDictionary.TryAdd("6", new HttpClientWrapper(new HttpClient()));
            _stringReRouteDictionary.TryAdd("7", new HttpClientWrapper(new HttpClient()));
            _stringReRouteDictionary.TryAdd("8", new HttpClientWrapper(new HttpClient()));
            _stringReRouteDictionary.TryAdd("9", new HttpClientWrapper(new HttpClient()));
            _stringReRouteDictionary.TryAdd("10", new HttpClientWrapper(new HttpClient()));
        }

        [Benchmark(Baseline = true)]
        public IHttpClient StringKey()
        {
            _stringReRouteDictionary.AddOrUpdate(_stringKey, _client, (k, oldValue) => _client);
            return _stringReRouteDictionary.TryGetValue(_stringKey, out var client) ? client : null;
        }

        [Benchmark]
        public IHttpClient DownstreamReRouteKey()
        {
            _downstreamReRouteDictionary.AddOrUpdate(_downstreamReRouteKey, _client, (k, oldValue) => _client);
            return _downstreamReRouteDictionary.TryGetValue(_downstreamReRouteKey, out var client) ? client : null;
        }
    }
}
