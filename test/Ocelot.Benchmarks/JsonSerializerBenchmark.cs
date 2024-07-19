using BenchmarkDotNet.Jobs;
using Bogus;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Ocelot.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [Config(typeof(JsonSerializerBenchmark))]
    public class JsonSerializerBenchmark : ManualConfig
    {
        private string _serializedTestUsers;

        private List<User> _testUsers = new();

        [Params(1000)]
        public int Count { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            Faker<User> faker = new Faker<User>().CustomInstantiator(
                f =>
                    new User
                    {
                        UserId = Guid.NewGuid(),
                        FirstName = f.Name.FirstName(),
                        LastName = f.Name.LastName(),
                        FullName = f.Name.FullName(),
                        Username = f.Internet.UserName(f.Name.FirstName(), f.Name.LastName()),
                        Email = f.Internet.Email(f.Name.FirstName(), f.Name.LastName())
                    }
            );

            _testUsers = faker.Generate(Count);

            _serializedTestUsers = JsonSerializer.Serialize(_testUsers);
        }

        [Benchmark]
        [BenchmarkCategory("Serialize", "Newtonsoft")]
        public void NewtonsoftSerializeBigData()
        {
            _ = JsonConvert.SerializeObject(_testUsers);
        }

        [Benchmark]
        [BenchmarkCategory("Serialize", "Microsoft")]
        public void MicrosoftSerializeBigData()
        {
            _ = JsonSerializer.Serialize(_testUsers);
        }

        [Benchmark]
        [BenchmarkCategory("Deserialize", "Newtonsoft")]
        public void NewtonsoftDeserializeBigData()
        {
            _ = JsonConvert.DeserializeObject<List<User>>(_serializedTestUsers);
        }

        [Benchmark]
        [BenchmarkCategory("Deserialize", "Microsoft")]
        public void MicrosoftDeserializeBigData()
        {
            _ = JsonSerializer.Deserialize<List<User>>(_serializedTestUsers);
        }
    }
}

public class User
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}
