namespace Ocelot.Configuration;

public class HeaderFindAndReplace
{
    public const char Comma = ',';

    public HeaderFindAndReplace(HeaderFindAndReplace from)
    {
        Key = from.Key;
        Find = from.Find;
        Replace = from.Replace;
        Index = from.Index;
    }

    public HeaderFindAndReplace(KeyValuePair<string, string> from)
    {
        Key = from.Key;
        string[] parsed = from.Value.Split(Comma);
        Find = parsed[0].Trim();
        Replace = parsed[1].Trim();
        Index = 0;
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

    public override string ToString() => $"{Key} at {Index}: {Find} → {Replace}";
}
