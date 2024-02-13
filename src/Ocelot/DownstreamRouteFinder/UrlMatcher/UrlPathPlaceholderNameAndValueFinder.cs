using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher
{
    public class UrlPathPlaceholderNameAndValueFinder : IPlaceholderNameAndValueFinder
    {
        public Response<List<PlaceholderNameAndValue>> Find(string path, string query, string pathTemplate)
        {
            var placeHolderNameAndValues = new List<PlaceholderNameAndValue>();

            path = $"{path}{query}";

            var counterForPath = 0;

            var delimiter = '/';
            var nextDelimiter = '/';

            for (var counterForTemplate = 0; counterForTemplate < pathTemplate.Length; counterForTemplate++)
            {
                if (ContinueScanningUrl(counterForPath, path.Length)
                    && CharactersDontMatch(pathTemplate[counterForTemplate], path[counterForPath])
                    && IsPlaceholder(pathTemplate[counterForTemplate]))
                {
                    //should_find_multiple_query_string make test pass
                    if (PassedQueryString(pathTemplate, counterForTemplate))
                    {
                        delimiter = '&';
                        nextDelimiter = '&';
                    }

                    //should_find_multiple_query_string_and_path makes test pass
                    if (NotPassedQueryString(pathTemplate, counterForTemplate) && NoMoreForwardSlash(pathTemplate, counterForTemplate))
                    {
                        delimiter = '?';
                        nextDelimiter = '?';
                    }

                    var placeholderName = GetPlaceholderName(pathTemplate, counterForTemplate);

                    var placeholderValue = GetPlaceholderValue(pathTemplate, query, placeholderName, path, counterForPath, delimiter);

                    placeHolderNameAndValues.Add(new PlaceholderNameAndValue(placeholderName, placeholderValue));

                    counterForTemplate = GetNextCounterPosition(pathTemplate, counterForTemplate, '}');

                    counterForPath = GetNextCounterPosition(path, counterForPath, nextDelimiter);

                    continue;
                }
                else if (IsCatchAll(path, counterForPath, pathTemplate) || IsCatchAllAfterOtherPlaceholders(pathTemplate, counterForTemplate))
                {
                    var endOfPlaceholder = GetNextCounterPosition(pathTemplate, counterForTemplate, '}');

                    var placeholderName = GetPlaceholderName(pathTemplate, counterForTemplate + 1);

                    if (NothingAfterFirstForwardSlash(path))
                    {
                        placeHolderNameAndValues.Add(new PlaceholderNameAndValue(placeholderName, string.Empty));
                    }
                    else
                    {
                        var placeholderValue = GetPlaceholderValue(pathTemplate, query, placeholderName, path, counterForPath, '?');
                        placeHolderNameAndValues.Add(new PlaceholderNameAndValue(placeholderName, placeholderValue));
                    }

                    counterForTemplate = endOfPlaceholder;
                    counterForPath = GetNextCounterPosition(path, counterForPath, '?');
                    continue;
                }

                counterForPath++;
            }

            return new OkResponse<List<PlaceholderNameAndValue>>(placeHolderNameAndValues);
        }

        private static bool NoMoreForwardSlash(string pathTemplate, int counterForTemplate)
        {
            return !pathTemplate.Substring(counterForTemplate).Contains("/");
        }

        private static bool NotPassedQueryString(string pathTemplate, int counterForTemplate)
        {
            return !pathTemplate.Substring(0, counterForTemplate).Contains('?');
        }

        private static bool PassedQueryString(string pathTemplate, int counterForTemplate)
        {
            return pathTemplate.Substring(0, counterForTemplate).Contains('?');
        }

        private static bool IsCatchAll(string path, int counterForPath, string pathTemplate)
        {
            return string.IsNullOrEmpty(path) || (path.Length > counterForPath && path[counterForPath] == '/') && pathTemplate.Length > 1
                     && pathTemplate.Substring(0, 2) == "/{"
                     && pathTemplate.IndexOf('}') == pathTemplate.Length - 1;
        }

        private static bool IsCatchAllAfterOtherPlaceholders(string pathTemplate, int counterForTemplate)
            => (pathTemplate[counterForTemplate] == '/' || pathTemplate[counterForTemplate] == '?')
                && (counterForTemplate < pathTemplate.Length - 1)
                && (pathTemplate[counterForTemplate + 1] == '{')
                && NoMoreForwardSlash(pathTemplate, counterForTemplate + 1);

        private static bool NothingAfterFirstForwardSlash(string path)
        {
            return path.Length == 1 || path.Length == 0;
        }

        private static string GetPlaceholderValue(string urlPathTemplate, string query, string variableName, string urlPath, int counterForUrl, char delimiter)
        {
            if (counterForUrl >= urlPath.Length)
            {
                return string.Empty;
            }

            if ( urlPath[counterForUrl] == '/')
            {
                counterForUrl++;
            }

            var positionOfNextSlash = urlPath.IndexOf(delimiter, counterForUrl);

            if (positionOfNextSlash == -1 || (urlPathTemplate.Trim(delimiter).EndsWith(variableName) && string.IsNullOrEmpty(query)))
            {
                positionOfNextSlash = urlPath.Length;
            }

            var variableValue = urlPath.Substring(counterForUrl, positionOfNextSlash - counterForUrl);

            return variableValue;
        }

        private static string GetPlaceholderName(string urlPathTemplate, int counterForTemplate)
        {
            var postitionOfPlaceHolderClosingBracket = urlPathTemplate.IndexOf('}', counterForTemplate) + 1;

            var variableName = urlPathTemplate.Substring(counterForTemplate, postitionOfPlaceHolderClosingBracket - counterForTemplate);

            return variableName;
        }

        private static int GetNextCounterPosition(string urlTemplate, int counterForTemplate, char delimiter)
        {
            var closingPlaceHolderPositionOnTemplate = urlTemplate.IndexOf(delimiter, counterForTemplate);
            return closingPlaceHolderPositionOnTemplate + 1;
        }

        private static bool CharactersDontMatch(char characterOne, char characterTwo)
        {
            return char.ToLower(characterOne) != char.ToLower(characterTwo);
        }

        private static bool ContinueScanningUrl(int counterForUrl, int urlLength)
        {
            return counterForUrl < urlLength;
        }

        private static bool IsPlaceholder(char character) => character == '{';
    }
}
