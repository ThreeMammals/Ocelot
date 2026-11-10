namespace Ocelot.Values;

public class DownstreamPathTemplate : Lazy<string>
{
    private readonly bool cached;
    private readonly Func<string> valueGetter;

    public DownstreamPathTemplate(string value)
        : base(value)
    {
        cached = true;
    }

    public DownstreamPathTemplate(Func<string> valueFactory, bool cacheValue = true)
        : base(valueFactory)
    {
        cached = cacheValue;
        valueGetter = valueFactory;
    }

    public new string Value
    {
        get => cached ? base.Value
            : valueGetter?.Invoke() ?? base.Value;
    }

    public override string ToString() => Value ?? string.Empty;
}
