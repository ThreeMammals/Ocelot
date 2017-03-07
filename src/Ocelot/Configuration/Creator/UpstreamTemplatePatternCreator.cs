using System.Collections.Generic;
using Ocelot.Configuration.File;
using Ocelot.Utilities;

namespace Ocelot.Configuration.Creator
{
    public class UpstreamTemplatePatternCreator : IUpstreamTemplatePatternCreator
    {
        private const string RegExMatchEverything = ".*";
        private const string RegExMatchEndString = "$";
        private const string RegExIgnoreCase = "(?i)";
        private const string RegExForwardSlashOnly = "^/$";

        public string Create(FileReRoute reRoute)
        {
            var upstreamTemplate = reRoute.UpstreamPathTemplate;

            upstreamTemplate = upstreamTemplate.SetLastCharacterAs('/');

            var placeholders = new List<string>();

            for (var i = 0; i < upstreamTemplate.Length; i++)
            {
                if (IsPlaceHolder(upstreamTemplate, i))
                {
                    var postitionOfPlaceHolderClosingBracket = upstreamTemplate.IndexOf('}', i);
                    var difference = postitionOfPlaceHolderClosingBracket - i + 1;
                    var variableName = upstreamTemplate.Substring(i, difference);
                    placeholders.Add(variableName);
                }
            }

            foreach (var placeholder in placeholders)
            {
                upstreamTemplate = upstreamTemplate.Replace(placeholder, RegExMatchEverything);
            }

            if (upstreamTemplate == "/")
            {
                return RegExForwardSlashOnly;
            }

            var route = reRoute.ReRouteIsCaseSensitive 
                ? $"{upstreamTemplate}{RegExMatchEndString}" 
                : $"{RegExIgnoreCase}{upstreamTemplate}{RegExMatchEndString}";

            return route;
        }


        private bool IsPlaceHolder(string upstreamTemplate, int i)
        {
            return upstreamTemplate[i] == '{';
        }
    }
}