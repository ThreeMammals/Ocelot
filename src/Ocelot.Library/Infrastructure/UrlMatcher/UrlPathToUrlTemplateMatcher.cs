using System;
using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.UrlMatcher
{
     public class UrlPathToUrlTemplateMatcher : IUrlPathToUrlTemplateMatcher
    {
        public UrlMatch Match(string upstreamUrlPath, string upstreamUrlTemplate)
        {
            var urlPathTemplateCopy = upstreamUrlTemplate;

            var templateKeysAndValues = new List<TemplateVariableNameAndValue>();

            int counterForUrl = 0;

            for (int counterForTemplate = 0; counterForTemplate < upstreamUrlTemplate.Length; counterForTemplate++)
            {
                if (CharactersDontMatch(upstreamUrlTemplate[counterForTemplate], upstreamUrlPath[counterForUrl]) && ContinueScanningUrl(counterForUrl,upstreamUrlPath.Length))
                {
                    if (IsPlaceholder(upstreamUrlTemplate[counterForTemplate]))
                    {
                        var variableName = GetPlaceholderVariableName(upstreamUrlTemplate, counterForTemplate);
                        
                        var variableValue = GetPlaceholderVariableValue(upstreamUrlPath, counterForUrl);

                        var templateVariableNameAndValue = new TemplateVariableNameAndValue(variableName, variableValue);

                        templateKeysAndValues.Add(templateVariableNameAndValue);

                        counterForTemplate = GetNextCounterPosition(upstreamUrlTemplate, counterForTemplate, '}');

                        counterForUrl = GetNextCounterPosition(upstreamUrlPath, counterForUrl, '/');

                        continue;
                    } 
                    else
                    {
                        return new UrlMatch(false, templateKeysAndValues, string.Empty);
                    } 
                }
                counterForUrl++;
            }
            return new UrlMatch(true, templateKeysAndValues, urlPathTemplateCopy);
        }

        private string GetPlaceholderVariableValue(string urlPath, int counterForUrl)
        {
            var positionOfNextSlash = urlPath.IndexOf('/', counterForUrl);

            if(positionOfNextSlash == -1)
            {
                positionOfNextSlash = urlPath.Length;
            }
            
            var variableValue = urlPath.Substring(counterForUrl, positionOfNextSlash - counterForUrl);

            return variableValue;
        }

        private string GetPlaceholderVariableName(string urlPathTemplate, int counterForTemplate)
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
            return characterOne != characterTwo;
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