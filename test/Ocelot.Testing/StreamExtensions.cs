namespace Ocelot.Testing;

public static class StreamExtensions
{
    public static string AsString(this Stream stream)
    {
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        return text;
    }
}
