using Ocelot.Configuration.File;
using Ocelot.Values;

namespace Ocelot.Configuration.Creator
{
    public class UpstreamTemplatePatternCreator : IUpstreamTemplatePatternCreator
    {
        public const string RegExMatchZeroOrMoreOfEverything = ".*";
        private const string RegExMatchOneOrMoreOfEverythingUntilNextForwardSlash = "[^/]+";
        private const string RegExMatchEndString = "$";
        private const string RegExIgnoreCase = "(?i)";
        private const string RegExForwardSlashOnly = "^/$";
        private const string RegExForwardSlashAndOnePlaceHolder = "^/.*";

        public UpstreamPathTemplate Create(IRoute route)
        {
            var upstreamTemplate = route.UpstreamPathTemplate;

            var placeholders = new List<string>();

            for (var i = 0; i < upstreamTemplate.Length; i++)
            {
                if (IsPlaceHolder(upstreamTemplate, i))
                {
                    var postitionOfPlaceHolderClosingBracket = upstreamTemplate.IndexOf('}', i);
                    var difference = postitionOfPlaceHolderClosingBracket - i + 1;
                    var placeHolderName = upstreamTemplate.Substring(i, difference);
                    placeholders.Add(placeHolderName);

                    //hack to handle /{url} case
                    if (ForwardSlashAndOnePlaceHolder(upstreamTemplate, placeholders, postitionOfPlaceHolderClosingBracket))
                    {
                        return new UpstreamPathTemplate(RegExForwardSlashAndOnePlaceHolder, 0, false, route.UpstreamPathTemplate);
                    }
                }
            }

            var containsQueryString = false;

            if (upstreamTemplate.Contains('?'))
            {
                containsQueryString = true;
                upstreamTemplate = upstreamTemplate.Replace("?", "(|\\?)");
            }

            for (var i = 0; i < placeholders.Count; i++)
            {
                var indexOfPlaceholder = upstreamTemplate.IndexOf(placeholders[i], StringComparison.Ordinal);
                var indexOfNextForwardSlash = upstreamTemplate.IndexOf("/", indexOfPlaceholder, StringComparison.Ordinal);
                if (indexOfNextForwardSlash < indexOfPlaceholder || (containsQueryString && upstreamTemplate.IndexOf('?', StringComparison.Ordinal) < upstreamTemplate.IndexOf(placeholders[i], StringComparison.Ordinal)))
                {
                    upstreamTemplate = upstreamTemplate.Replace(placeholders[i], RegExMatchZeroOrMoreOfEverything);
                }
                else
                {
                    upstreamTemplate = upstreamTemplate.Replace(placeholders[i], RegExMatchOneOrMoreOfEverythingUntilNextForwardSlash);
                }
            }

            if (upstreamTemplate == "/")
            {
                return new UpstreamPathTemplate(RegExForwardSlashOnly, route.Priority, containsQueryString, route.UpstreamPathTemplate);
            }

            var index = upstreamTemplate.LastIndexOf('/'); // index of last forward slash
            if (index < (upstreamTemplate.Length - 1) && upstreamTemplate[index + 1] == '.')
            {
                upstreamTemplate = upstreamTemplate[..index] + "(?:|/" + upstreamTemplate[++index..] + ")";
            }

            if (upstreamTemplate.EndsWith("/"))
            {
                upstreamTemplate = upstreamTemplate.Remove(upstreamTemplate.Length - 1, 1) + "(/|)";
            }

            var template = route.RouteIsCaseSensitive
                ? $"^{upstreamTemplate}{RegExMatchEndString}"
                : $"^{RegExIgnoreCase}{upstreamTemplate}{RegExMatchEndString}";

            return new UpstreamPathTemplate(template, route.Priority, containsQueryString, route.UpstreamPathTemplate);
        }

        private static bool ForwardSlashAndOnePlaceHolder(string upstreamTemplate, List<string> placeholders, int postitionOfPlaceHolderClosingBracket)
        {
            if (upstreamTemplate.Substring(0, 2) == "/{" && placeholders.Count == 1 && upstreamTemplate.Length == postitionOfPlaceHolderClosingBracket + 1)
            {
                return true;
            }

            return false;
        }

        private static bool IsPlaceHolder(string upstreamTemplate, int i)
        {
            return upstreamTemplate[i] == '{';
        }
    }
}
