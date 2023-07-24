using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Requester;
using System.Collections.Concurrent;

namespace Ocelot.Benchmarks
{
    [Config(typeof(DictionaryBenchmarks))]
    public class DictionaryBenchmarks : ManualConfig
    {
        private ConcurrentDictionary<DownstreamRoute, IHttpClient> _downstreamRouteDictionary;
        private ConcurrentDictionary<string, IHttpClient> _stringRouteDictionary;
        private HttpClientWrapper _client;
        private string _stringKey;
        private DownstreamRoute _downstreamRouteKey;

        public DictionaryBenchmarks()
        {
            AddColumn(StatisticColumn.AllStatistics);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddValidator(BaselineValidator.FailOnError);
        }

        [GlobalSetup]
        public void SetUp()
        {
            _downstreamRouteKey = new DownstreamRouteBuilder().Build();
            _stringKey = "test";
            _client = new HttpClientWrapper(new HttpClient());
            _downstreamRouteDictionary = new ConcurrentDictionary<DownstreamRoute, IHttpClient>();

            _downstreamRouteDictionary.TryAdd(new DownstreamRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamRouteDictionary.TryAdd(new DownstreamRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamRouteDictionary.TryAdd(new DownstreamRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamRouteDictionary.TryAdd(new DownstreamRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamRouteDictionary.TryAdd(new DownstreamRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamRouteDictionary.TryAdd(new DownstreamRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamRouteDictionary.TryAdd(new DownstreamRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamRouteDictionary.TryAdd(new DownstreamRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamRouteDictionary.TryAdd(new DownstreamRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));
            _downstreamRouteDictionary.TryAdd(new DownstreamRouteBuilder().Build(), new HttpClientWrapper(new HttpClient()));

            _stringRouteDictionary = new ConcurrentDictionary<string, IHttpClient>();
            _stringRouteDictionary.TryAdd("1", new HttpClientWrapper(new HttpClient()));
            _stringRouteDictionary.TryAdd("2", new HttpClientWrapper(new HttpClient()));
            _stringRouteDictionary.TryAdd("3", new HttpClientWrapper(new HttpClient()));
            _stringRouteDictionary.TryAdd("4", new HttpClientWrapper(new HttpClient()));
            _stringRouteDictionary.TryAdd("5", new HttpClientWrapper(new HttpClient()));
            _stringRouteDictionary.TryAdd("6", new HttpClientWrapper(new HttpClient()));
            _stringRouteDictionary.TryAdd("7", new HttpClientWrapper(new HttpClient()));
            _stringRouteDictionary.TryAdd("8", new HttpClientWrapper(new HttpClient()));
            _stringRouteDictionary.TryAdd("9", new HttpClientWrapper(new HttpClient()));
            _stringRouteDictionary.TryAdd("10", new HttpClientWrapper(new HttpClient()));
        }

        [Benchmark(Baseline = true)]
        public IHttpClient StringKey()
        {
            _stringRouteDictionary.AddOrUpdate(_stringKey, _client, (k, oldValue) => _client);
            return _stringRouteDictionary.TryGetValue(_stringKey, out var client) ? client : null;
        }

        [Benchmark]
        public IHttpClient DownstreamRouteKey()
        {
            _downstreamRouteDictionary.AddOrUpdate(_downstreamRouteKey, _client, (k, oldValue) => _client);
            return _downstreamRouteDictionary.TryGetValue(_downstreamRouteKey, out var client) ? client : null;
        }
    }
}
