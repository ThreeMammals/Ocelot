using System;
using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.UrlPathMatcher
{
     public class UrlPathToUrlPathTemplateMatcher : IUrlPathToUrlPathTemplateMatcher
    {
        public UrlPathMatch Match(string urlPath, string urlPathTemplate)
        {
            var templateKeysAndValues = new List<TemplateVariableNameAndValue>();

            urlPath = urlPath.ToLower();

            urlPathTemplate = urlPathTemplate.ToLower();

            int counterForUrl = 0;

            for (int counterForTemplate = 0; counterForTemplate < urlPathTemplate.Length; counterForTemplate++)
            {
                if (CharactersDontMatch(urlPathTemplate[counterForTemplate], urlPath[counterForUrl]) && ContinueScanningUrl(counterForUrl,urlPath.Length))
                {
                    if (IsPlaceholder(urlPathTemplate[counterForTemplate]))
                    {
                        var variableName = GetPlaceholderVariableName(urlPathTemplate, counterForTemplate);
                        
                        var variableValue = GetPlaceholderVariableValue(urlPath, counterForUrl);

                        var templateVariableNameAndValue = new TemplateVariableNameAndValue(variableName, variableValue);

                        templateKeysAndValues.Add(templateVariableNameAndValue);

                        counterForTemplate = GetNextCounterPosition(urlPathTemplate, counterForTemplate, '}');

                        counterForUrl = GetNextCounterPosition(urlPath, counterForUrl, '/');

                        continue;
                    } 
                    else
                    {
                        return new UrlPathMatch(false, templateKeysAndValues);
                    } 
                }
                counterForUrl++;
            }
            return new UrlPathMatch(true, templateKeysAndValues);
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