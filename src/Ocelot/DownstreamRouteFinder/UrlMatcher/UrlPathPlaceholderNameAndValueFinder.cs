using Ocelot.Infrastructure;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher;

/// <summary>The finder locates all occurrences of placeholders' names and values within URL paths.
/// <para>This is the default implementation of the <see cref="IPlaceholderNameAndValueFinder"/> interface.</para>
/// </summary>
public partial class UrlPathPlaceholderNameAndValueFinder : IPlaceholderNameAndValueFinder
{
    private const char LeftBrace = '{';
    private const char RightBrace = '}';

    /// <summary>Finds the placeholders in the request path and query and returns their matching values.
    /// <para>We might encounter the following scenarios:
    /// <list type="bullet">
    /// <item>The path template contains a Catch-All query parameter. If so, we return the Catch-All placeholder with an empty value.</item>
    /// <item>The path template contains a Catch-All path parameter. If so, we return the Catch-All placeholder with an empty value.</item>
    /// <item>The path template contains placeholders. We return the placeholders with their matching values.</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <param name="query">The query parameters.</param>
    /// <param name="pathTemplate">The request path template.</param>
    /// <returns>A <see cref="List{PlaceholderNameAndValue}"/> object, where T is <see cref="PlaceholderNameAndValue"/>: the list of the placeholders with their matching values.</returns>
    public Response<List<PlaceholderNameAndValue>> Find(string path, string query, string pathTemplate)
    {
        (bool isCatchAllQuery, string catchAllQueryPlaceholder) = IsCatchAllQuery(pathTemplate);
        if (isCatchAllQuery)
        {
            return new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>
            {
                new($"{LeftBrace}{catchAllQueryPlaceholder}{RightBrace}", string.Empty),
            });
        }

        // Find matching groups from path and query
        var placeholders = FindGroups(path, query, pathTemplate)
            .Select(g => new PlaceholderNameAndValue($"{LeftBrace}{g.Name}{RightBrace}", g.Value))
            .ToList();
        return new OkResponse<List<PlaceholderNameAndValue>>(placeholders);
    }

    private const int PlaceholdersMilliseconds = 1000;
    [GeneratedRegex(@"\{(.*?)\}", RegexOptions.None, PlaceholdersMilliseconds)]
    private static partial Regex RegexPlaceholders();

    /// <summary>Finds the placeholders in the request path and query.
    /// We use a <see cref="Regex"/> pattern to match the placeholders in the path template.
    /// <para>We have two slight optimizations:
    /// <list type="number">
    /// <item>First, we skip the query if it is not present in the path template.</item>
    /// <item>Second, we append a trailing slash to the path if it is a Catch-All path.</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <param name="query">The query parameters.</param>
    /// <param name="template">The path template.</param>
    /// <returns>A <see cref="List{Group}"/> object (T is <see cref="Group"/>): the matching groups.</returns>
    private static List<Group> FindGroups(string path, string query, string template)
    {
        template = EscapeExceptBraces(template);
        var regexPattern = GenerateRegexPattern(template);
        var testedPath = ShouldSkipQuery(query, template) ? path : $"{path}{query}";
        
        var match = Regex.Match(testedPath, regexPattern);
        var foundGroups = match.Groups.Cast<Group>().Skip(1).ToList();
        
        if (foundGroups.Count > 0 || !IsCatchAllPath(template))
        {
            return foundGroups;
        }

        // Append a trailing slash to the path if it is a catch-all path
        match = Regex.Match($"{testedPath}/", regexPattern);
        return match.Groups.Cast<Group>().Skip(1).ToList();
    }
    
    /// <summary>
    /// The placeholders that are not placed at the end of the template are delimited by forward slashes, only the last one, the catch-all can match more segments.
    /// </summary>
    /// <param name="escapedTemplate">The escaped path template.</param>
    /// <returns>The pattern for values replacement.</returns>
    private static string GenerateRegexPattern(string escapedTemplate)
    {
        // First we count the matches
        var placeHoldersCountMatch = RegexPlaceholders().Matches(escapedTemplate);
        int index = 0, placeHoldersCount = placeHoldersCountMatch.Count;

        // We know that the replace process will be started from the beginning of the url,
        // so we can use a simple counter to determine the last placeholder
        string MatchEvaluator(Match match)
        {
            var groupName = match.Groups[1].Value;
            index++;
            return index == placeHoldersCount ? $"(?<{groupName}>[^&]*)" : $"(?<{groupName}>[^/|&]*)";
        }

        return $@"^{RegexPlaceholders().Replace(escapedTemplate, MatchEvaluator)}";
    }

    private const int CatchAllQueryMilliseconds = 300;
    [GeneratedRegex(@"^[^{{}}]*\?\{(.*?)\}$", RegexOptions.None, CatchAllQueryMilliseconds)]
    private static partial Regex RegexCatchAllQuery();

    /// <summary>Checks if the path template contains a Catch-All query parameter.
    /// <para>It means that the path template ends with a question mark and a placeholder.
    /// And no other placeholders are present in the path template.</para>
    /// </summary>
    /// <param name="template">The path template.</param>
    /// <returns><see langword="true"/> if it matches and the found placeholder.</returns>
    private static (bool IsMatch, string Placeholder) IsCatchAllQuery(string template)
    {
        var catchAllMatch = RegexCatchAllQuery().Match(template);
        return (catchAllMatch.Success,
            catchAllMatch.Success ? catchAllMatch.Groups[1].Value : string.Empty);
    }

    private const int CatchAllPathMilliseconds = 300;
    [GeneratedRegex(@"^[^{{}}]*\{(.*?)\}/?$", RegexOptions.None, CatchAllPathMilliseconds)]
    private static partial Regex RegexCatchAllPath();

    /// <summary>Check if the path template contains a Catch-All path parameter.
    /// <para>It means that the path template ends with a placeholder and no other placeholders are present in the path template, without a question mark (query parameters).</para>
    /// </summary>
    /// <param name="template">The path template.</param>
    /// <returns><see langword="true"/> if it matches.</returns>
    private static bool IsCatchAllPath(string template) => RegexCatchAllPath().IsMatch(template) && !template.Contains(@"\?");

    /// <summary>Checks if the query should be skipped.
    /// <para>It should be skipped if it is not present in the path template.
    /// Since the template is escaped, looking for \? not only ?.</para>
    /// </summary>
    /// <param name="query">The query string.</param>
    /// <param name="template">The path template.</param>
    /// <returns><see langword="true"/> if query should be skipped.</returns>
    private static bool ShouldSkipQuery(string query, string template) => !string.IsNullOrEmpty(query) && !template.Contains(@"\?");

    /// <summary>Escapes all characters except braces, eg { and }.</summary>
    /// <param name="input">The input string.</param>
    /// <returns>The formatted <see cref="string"/>.</returns>
    private static string EscapeExceptBraces(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        StringBuilder escaped = new();
        foreach (char c in input.AsSpan())
        {
            if (c is LeftBrace or RightBrace)
            {
                escaped.Append(c);
            }
            else
            {
                escaped.Append(Regex.Escape(c.ToString()));
            }
        }

        // Here we are not interested in the path itself, only the placeholders.
        // Path validation is not part of this class, therefore allowing case-insensitive 
        // matching.
        return $"^(?i){escaped}";
    }
}
