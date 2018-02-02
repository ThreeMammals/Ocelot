using System.Collections.Generic;
using Ocelot.Configuration.File;
using Ocelot.Values;

namespace Ocelot.Configuration.Creator
{
    public class UpstreamTemplatePatternCreator : IUpstreamTemplatePatternCreator
    {
        private const string RegExMatchEverything = "[0-9a-zA-Z].*";
        private const string RegExMatchEndString = "$";
        private const string RegExIgnoreCase = "(?i)";
        private const string RegExForwardSlashOnly = "^/$";
        private const string RegExForwardSlashAndOnePlaceHolder = "^/.*";

        public UpstreamPathTemplate Create(FileReRoute reRoute)
        {
            var upstreamTemplate = reRoute.UpstreamPathTemplate;

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
                    if(ForwardSlashAndOnePlaceHolder(upstreamTemplate, placeholders, postitionOfPlaceHolderClosingBracket))
                    {
                        return new UpstreamPathTemplate(RegExForwardSlashAndOnePlaceHolder, 0);
                    }
                }
            }

            foreach (var placeholder in placeholders)
            {
                upstreamTemplate = upstreamTemplate.Replace(placeholder, RegExMatchEverything);
            }

            if (upstreamTemplate == "/")
            {
                return new UpstreamPathTemplate(RegExForwardSlashOnly, 1);
            }

            if(upstreamTemplate.EndsWith("/"))
            {
                upstreamTemplate = upstreamTemplate.Remove(upstreamTemplate.Length -1, 1) + "(/|)";
            }

            var route = reRoute.ReRouteIsCaseSensitive 
                ? $"^{upstreamTemplate}{RegExMatchEndString}" 
                : $"^{RegExIgnoreCase}{upstreamTemplate}{RegExMatchEndString}";

            return new UpstreamPathTemplate(route, 1);
        }

        private bool ForwardSlashAndOnePlaceHolder(string upstreamTemplate, List<string> placeholders, int postitionOfPlaceHolderClosingBracket)
        {
            if(upstreamTemplate.Substring(0, 2) == "/{" && placeholders.Count == 1 && upstreamTemplate.Length == postitionOfPlaceHolderClosingBracket + 1)
            {   
                return true;
            }

            return false;
        }


        private bool IsPlaceHolder(string upstreamTemplate, int i)
        {
            return upstreamTemplate[i] == '{';
        }
    }
}