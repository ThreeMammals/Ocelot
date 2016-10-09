using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlMatcher
{
     public class TemplateVariableNameAndValueFinder : ITemplateVariableNameAndValueFinder
    {
        public Response<List<TemplateVariableNameAndValue>> Find(string upstreamUrlPath, string upstreamUrlPathTemplate)
        {
            var templateKeysAndValues = new List<TemplateVariableNameAndValue>();

            int counterForUrl = 0;
         
            for (int counterForTemplate = 0; counterForTemplate < upstreamUrlPathTemplate.Length; counterForTemplate++)
            {
                if (CharactersDontMatch(upstreamUrlPathTemplate[counterForTemplate], upstreamUrlPath[counterForUrl]) && ContinueScanningUrl(counterForUrl,upstreamUrlPath.Length))
                {
                    if (IsPlaceholder(upstreamUrlPathTemplate[counterForTemplate]))
                    {
                        var variableName = GetPlaceholderVariableName(upstreamUrlPathTemplate, counterForTemplate);
                        
                        var variableValue = GetPlaceholderVariableValue(upstreamUrlPath, counterForUrl);

                        var templateVariableNameAndValue = new TemplateVariableNameAndValue(variableName, variableValue);

                        templateKeysAndValues.Add(templateVariableNameAndValue);

                        counterForTemplate = GetNextCounterPosition(upstreamUrlPathTemplate, counterForTemplate, '}');

                        counterForUrl = GetNextCounterPosition(upstreamUrlPath, counterForUrl, '/');

                        continue;
                    }

                    return new OkResponse<List<TemplateVariableNameAndValue>>(templateKeysAndValues);
                }
                counterForUrl++;
            }

            return new OkResponse<List<TemplateVariableNameAndValue>>(templateKeysAndValues);
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