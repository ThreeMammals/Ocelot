namespace Ocelot.Library.Infrastructure.Router.UrlPathMatcher
{
     public class UrlPathToUrlPathTemplateMatcher : IUrlPathToUrlPathTemplateMatcher
    {
        public bool Match(string urlPath, string urlPathTemplate)
        {
            urlPath = urlPath.ToLower();

            urlPathTemplate = urlPathTemplate.ToLower();

            int counterForUrl = 0;

            for (int counterForTemplate = 0; counterForTemplate < urlPathTemplate.Length; counterForTemplate++)
            {
                if (CharactersDontMatch(urlPathTemplate[counterForTemplate], urlPath[counterForUrl]) && ContinueScanningUrl(counterForUrl,urlPath.Length))
                {
                    if (IsPlaceholder(urlPathTemplate[counterForTemplate]))
                    {
                        counterForTemplate = GetNextCounterPosition(urlPathTemplate, counterForTemplate, '}');
                        counterForUrl = GetNextCounterPosition(urlPath, counterForUrl, '/');
                        continue;
                    } 
                    else
                    {
                        return false;
                    } 
                }
                counterForUrl++;
            }
            return true;
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