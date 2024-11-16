using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher;

public class UrlPathPlaceholderNameAndValueFinder : IPlaceholderNameAndValueFinder
{
    private const char LeftCurlyBracket = '{';
    private const char RightCurlyBracket = '}';

    /// <summary>
    /// Finds the placeholders in the request path and query and returns their matching values.
    /// We might encounter the following scenarios:
    /// - The path template contains a catch-all query parameter. If so, we return the catch-all placeholder with an empty value.
    /// - The path template contains a catch-all path parameter. If so, we return the catch-all placeholder with an empty value.
    /// - The path template contains placeholders. We return the placeholders with their matching values.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <param name="query">The query parameters.</param>
    /// <param name="pathTemplate">The request path template.</param>
    /// <returns>The list of the placeholders with their matching values.</returns>
    public Response<List<PlaceholderNameAndValue>> Find(string path, string query, string pathTemplate)
    {
        (bool isCatchAllQuery, string catchAllQueryPlaceholder) = IsCatchAllQuery(pathTemplate);
        if (isCatchAllQuery)
        {
            return new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>
            {
                new($"{LeftCurlyBracket}{catchAllQueryPlaceholder}{RightCurlyBracket}", string.Empty),
            });
        }

        // Find matching groups from path and query
        IList<Group> groups = FindGroups(path, query, pathTemplate);
        List<PlaceholderNameAndValue> placeholderNameAndValues = groups
            .Select(group => new PlaceholderNameAndValue($"{LeftCurlyBracket}{group.Name}{RightCurlyBracket}", group.Value))
            .ToList();

        return new OkResponse<List<PlaceholderNameAndValue>>(placeholderNameAndValues);
    }

    /// <summary>
    /// Finds the placeholders in the request path and query.
    /// We use a regex pattern to match the placeholders in the path template.
    /// We have two slight optimizations:
    /// - First, we skip the query if it is not present in the path template.
    /// - Second, we append a trailing slash to the path if it is a catch-all path.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <param name="query">The query parameters.</param>
    /// <param name="template">The path template.</param>
    /// <returns>The matching groups.</returns>
    private static IList<Group> FindGroups(string path, string query, string template)
    {
        Regex regex = new(@"\{(.*?)\}", RegexOptions.None, TimeSpan.FromSeconds(1));
        template = EscapeExceptBraces(template);
        string regexPattern = $"^{regex.Replace(template, match => $"(?<{match.Groups[1].Value}>[^&]*)")}";
        
        string testedPath = ShouldSkipQuery(query, template) ? path : $"{path}{query}";
        Match match = Regex.Match(testedPath, regexPattern);
        List<Group> foundGroups = match.Groups.Cast<Group>().Skip(1).ToList();
        
        if (foundGroups.Count > 0)
        {
            return foundGroups;
        }

        if (!IsCatchAllPath(template))
        {
            return foundGroups;
        }

        // Append a trailing slash to the path if it is a catch-all path
        match = Regex.Match($"{testedPath}/", regexPattern);
        return match.Groups.Cast<Group>().Skip(1).ToList();
    }

    /// <summary>
    /// Checks if the path template contains a catch-all query parameter.
    /// It means that the path template ends with a question mark and a placeholder.
    /// And no other placeholders are present in the path template.
    /// </summary>
    /// <param name="template">The path template.</param>
    /// <returns>True if it matches and the found placeholder.</returns>
    private static (bool IsMatch, string Placeholder) IsCatchAllQuery(string template)
    {
        Regex catchAllQueryRegex = new(@"^[^{{}}]*\?\{{(.*?)\}}$", RegexOptions.None, TimeSpan.FromMilliseconds(300));
        Match catchAllMatch = catchAllQueryRegex.Match(template);

        return (catchAllMatch.Success, catchAllMatch.Success ? catchAllMatch.Groups[1].Value : string.Empty);
    }

    /// <summary>
    /// Check if the path template contains a catch-all path parameter.
    /// It means that the path template ends with a placeholder and no other placeholders are present in the path template,
    /// without a question mark (query parameters).
    /// </summary>
    /// <param name="template">The path template.</param>
    /// <returns>True if it matches.</returns>
    private static bool IsCatchAllPath(string template)
    {
        Regex catchAllPathRegex = new(@"^[^{{}}]*\{{(.*?)\}}/?$", RegexOptions.None, TimeSpan.FromMilliseconds(300));
        return catchAllPathRegex.IsMatch(template) && !template.Contains('?');
    }
    
    /// <summary>
    /// Checks if the query should be skipped.
    /// It should be skipped if it is not present in the path template.
    /// </summary>
    /// <param name="query">The query string.</param>
    /// <param name="template">The path template.</param>
    /// <returns>True if query should be skipped.</returns>
    private static bool ShouldSkipQuery(string query, string template) => !string.IsNullOrEmpty(query) && !template.Contains('?');

    /// <summary>
    /// Escapes all characters except braces, eg { and }.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The formatted string.</returns>
    private static string EscapeExceptBraces(string input)
    {
        StringBuilder escaped = new();
        ReadOnlySpan<char> span = input.AsSpan();
 
        foreach (char c in span)
        {
            if (c is LeftCurlyBracket or RightCurlyBracket)
            {
                escaped.Append(c);
            }
            else
            {
                escaped.Append(Regex.Escape(c.ToString()));
            }
        }
 
        return escaped.ToString();
    }
}
