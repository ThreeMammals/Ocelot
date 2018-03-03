using System.Collections.Generic;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher
{
    public class UrlPathPlaceholderNameAndValueFinder : IPlaceholderNameAndValueFinder
    {
        public Response<List<PlaceholderNameAndValue>> Find(string path, string pathTemplate)
        {
            var placeHolderNameAndValues = new List<PlaceholderNameAndValue>();

            int counterForPath = 0;
         
            for (int counterForTemplate = 0; counterForTemplate < pathTemplate.Length; counterForTemplate++)
            {
                if ((path.Length > counterForPath) && CharactersDontMatch(pathTemplate[counterForTemplate], path[counterForPath]) && ContinueScanningUrl(counterForPath,path.Length))
                {
                    if (IsPlaceholder(pathTemplate[counterForTemplate]))
                    {
                        var placeholderName = GetPlaceholderName(pathTemplate, counterForTemplate);

                        var placeholderValue = GetPlaceholderValue(pathTemplate, placeholderName, path, counterForPath);

                        placeHolderNameAndValues.Add(new PlaceholderNameAndValue(placeholderName, placeholderValue));

                        counterForTemplate = GetNextCounterPosition(pathTemplate, counterForTemplate, '}');

                        counterForPath = GetNextCounterPosition(path, counterForPath, '/');

                        continue;
                    }

                    return new OkResponse<List<PlaceholderNameAndValue>>(placeHolderNameAndValues);
                }
                else if(IsCatchAll(path, counterForPath, pathTemplate))
                {
                    var endOfPlaceholder = GetNextCounterPosition(pathTemplate, counterForTemplate, '}');

                    var placeholderName = GetPlaceholderName(pathTemplate, 1);

                    if(NothingAfterFirstForwardSlash(path))
                    {
                        placeHolderNameAndValues.Add(new PlaceholderNameAndValue(placeholderName, ""));
                    }
                    else
                    {
                        var placeholderValue = GetPlaceholderValue(pathTemplate, placeholderName, path, counterForPath + 1);
                        placeHolderNameAndValues.Add(new PlaceholderNameAndValue(placeholderName, placeholderValue));
                    }

                    counterForTemplate = endOfPlaceholder;
                }

                counterForPath++;
            }

            return new OkResponse<List<PlaceholderNameAndValue>>(placeHolderNameAndValues);
        }

        private bool IsCatchAll(string path, int counterForPath, string pathTemplate)
        {
            return string.IsNullOrEmpty(path) || (path.Length > counterForPath && path[counterForPath] == '/') && pathTemplate.Length > 1 
                     && pathTemplate.Substring(0, 2) == "/{" 
                     && pathTemplate.IndexOf('}') == pathTemplate.Length - 1;
        }

        private bool NothingAfterFirstForwardSlash(string path)
        {
            return path.Length == 1 || path.Length == 0;
        }

        private string GetPlaceholderValue(string urlPathTemplate, string variableName, string urlPath, int counterForUrl)
        {
            var positionOfNextSlash = urlPath.IndexOf('/', counterForUrl);

            if (positionOfNextSlash == -1 || urlPathTemplate.Trim('/').EndsWith(variableName))
            {
                positionOfNextSlash = urlPath.Length;
            }

            var variableValue = urlPath.Substring(counterForUrl, positionOfNextSlash - counterForUrl);

            return variableValue;
        }

        private string GetPlaceholderName(string urlPathTemplate, int counterForTemplate)
        {
            var postitionOfPlaceHolderClosingBracket = urlPathTemplate.IndexOf('}', counterForTemplate) + 1;

            var variableName = urlPathTemplate.Substring(counterForTemplate, postitionOfPlaceHolderClosingBracket - counterForTemplate);

            return variableName;
        }

        private int GetNextCounterPosition(string urlTemplate, int counterForTemplate, char delimiter)
        {                        
            var closingPlaceHolderPositionOnTemplate = urlTemplate.IndexOf(delimiter, counterForTemplate);
            return closingPlaceHolderPositionOnTemplate + 1; 
        }

        private bool CharactersDontMatch(char characterOne, char characterTwo)
        {
            return char.ToLower(characterOne) != char.ToLower(characterTwo);
        }

        private bool ContinueScanningUrl(int counterForUrl, int urlLength)
        {
            return counterForUrl < urlLength;
        }

        private bool IsPlaceholder(char character)
        {
            return character == '{';
        }
    }
}
