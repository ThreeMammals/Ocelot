namespace Ocelot.Samples.Metadata.Models;

public class TestDeflateResponse
{
    public bool? deflated { get; set; }
    public Dictionary<string, string>? headers { get; set; }
    public string? method { get; set; }
}
