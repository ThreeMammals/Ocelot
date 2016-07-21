using System;
using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.UrlPathMatcher
{
     public class UrlPathToUrlPathTemplateMatcher : IUrlPathToUrlPathTemplateMatcher
    {
        public UrlPathMatch Match(string downstreamUrlPath, string downstreamUrlPathTemplate)
        {
            var urlPathTemplateCopy = downstreamUrlPathTemplate;

            var templateKeysAndValues = new List<TemplateVariableNameAndValue>();

            int counterForUrl = 0;

            for (int counterForTemplate = 0; counterForTemplate < downstreamUrlPathTemplate.Length; counterForTemplate++)
            {
                if (CharactersDontMatch(downstreamUrlPathTemplate[counterForTemplate], downstreamUrlPath[counterForUrl]) && ContinueScanningUrl(counterForUrl,downstreamUrlPath.Length))
                {
                    if (IsPlaceholder(downstreamUrlPathTemplate[counterForTemplate]))
                    {
                        var variableName = GetPlaceholderVariableName(downstreamUrlPathTemplate, counterForTemplate);
                        
                        var variableValue = GetPlaceholderVariableValue(downstreamUrlPath, counterForUrl);

                        var templateVariableNameAndValue = new TemplateVariableNameAndValue(variableName, variableValue);

                        templateKeysAndValues.Add(templateVariableNameAndValue);

                        counterForTemplate = GetNextCounterPosition(downstreamUrlPathTemplate, counterForTemplate, '}');

                        counterForUrl = GetNextCounterPosition(downstreamUrlPath, counterForUrl, '/');

                        continue;
                    } 
                    else
                    {
                        return new UrlPathMatch(false, templateKeysAndValues, string.Empty);
                    } 
                }
                counterForUrl++;
            }
            return new UrlPathMatch(true, templateKeysAndValues, urlPathTemplateCopy);
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