namespace Ocelot.Samples.Metadata.Models;

public class TestGZipResponse
{
    public bool? gzipped { get; set; }
    public Dictionary<string, string>? headers { get; set; }
    public string? method { get; set; }
}
