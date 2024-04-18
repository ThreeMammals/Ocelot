namespace Ocelot.Values;

/// <summary>
/// Upstream template properties of headers and their regular expression.
/// </summary>
/// <remarks>Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/routing.rst#upstream-headers">Routing based on request header</see>.</remarks>
public class UpstreamHeaderTemplate
{
    public string Template { get; }
    public string OriginalValue { get; }
    public Regex Pattern { get; }

    public UpstreamHeaderTemplate(string template, string originalValue)
    {
        Template = template;
        OriginalValue = originalValue;
        Pattern = new Regex(template ?? "$^", RegexOptions.Compiled | RegexOptions.Singleline);
    }
}
