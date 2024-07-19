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

    //BenchmarkDotNet v0.13.11, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
    //Intel Core i7-10870H CPU 2.20GHz, 1 CPU, 16 logical and 8 physical cores
    //    .NET SDK 8.0.303
    //    [Host]   : .NET 6.0.32 (6.0.3224.31407), X64 RyuJIT AVX2[AttachedDebugger]
    //    .NET 8.0 : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2

    //    Job =.NET 8.0  Runtime=.NET 8.0

    //    | Method                       | Count | Mean       | Error    | StdDev    | Median     | Op/s    | Gen0     | Gen1     | Gen2     | Allocated |
    //    |----------------------------- |------ |-----------:|---------:|----------:|-----------:|--------:|---------:|---------:|---------:|----------:|
    //    | MicrosoftDeserializeBigData  | 1000  |   856.3 us | 53.98 us | 157.47 us |   797.1 us | 1,167.8 |  39.0625 |  13.6719 |        - | 328.78 KB |
    //    | NewtonsoftDeserializeBigData | 1000  | 1,137.2 us | 18.74 us |  17.53 us | 1,132.8 us |   879.4 |  54.6875 |  17.5781 |        - | 457.94 KB |
    //    |==============================================================================================================================================|
    //    | MicrosoftSerializeBigData    | 1000  |   646.4 us | 12.72 us |  20.90 us |   645.7 us | 1,546.9 | 110.3516 | 110.3516 | 110.3516 | 350.02 KB |
    //    | NewtonsoftSerializeBigData   | 1000  | 1,033.4 us | 19.37 us |  42.53 us | 1,022.8 us |   967.7 | 109.3750 | 109.3750 | 109.3750 | 837.82 KB |
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
