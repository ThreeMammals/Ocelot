namespace Ocelot.Configuration;

public class HeaderFindAndReplace
{
    public const char Comma = ',';

    public HeaderFindAndReplace(HeaderFindAndReplace from)
    {
        ArgumentNullException.ThrowIfNull(from, nameof(from));
        Index = from.Index;
        Key = from.Key;
        Find = from.Find;
        Replace = from.Replace;
    }

    public HeaderFindAndReplace(KeyValuePair<string, string> from)
    {
        Index = 0;
        Key = from.Key;
        if (!string.IsNullOrWhiteSpace(from.Value) && from.Value.Contains(Comma))
        {
            string[] parsed = from.Value.Split(Comma);
            Find = parsed[0].Trim();
            Replace = parsed[1].Trim();
        }
        else
        {
            Find = from.Value?.Trim() ?? string.Empty;
            Replace = string.Empty;
        }
    }

    public HeaderFindAndReplace(string key, string find, string replace, int index)
    {
        Key = key;
        Find = find;
        Replace = replace;
        Index = index;
    }

    public string Key { get; }
    public string Find { get; }
    public string Replace { get; }

    // only index 0 for now..
    public int Index { get; }

    public override string ToString() => $"{Key} at {Index}: {Find} -> {Replace}";
}
