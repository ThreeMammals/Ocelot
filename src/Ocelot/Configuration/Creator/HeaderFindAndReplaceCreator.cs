using System;
using System.Collections.Generic;
using Ocelot.Configuration.File;
using Ocelot.Middleware;

namespace Ocelot.Configuration.Creator
{
    public class HeaderFindAndReplaceCreator : IHeaderFindAndReplaceCreator
    {
        private IBaseUrlFinder _finder;
        private Dictionary<string, Func<string>> _placeholders;

        public HeaderFindAndReplaceCreator(IBaseUrlFinder finder)
        {
            _finder = finder;
            _placeholders = new Dictionary<string, Func<string>>();
            _placeholders.Add("{BaseUrl}", () => {
                return _finder.Find();
            });
        }

        public HeaderTransformations Create(FileReRoute fileReRoute)
        {
            var upstream = new List<HeaderFindAndReplace>();

            foreach(var input in fileReRoute.UpstreamHeaderTransform)
            {
                var hAndr = Map(input);
                upstream.Add(hAndr);
            }

            var downstream = new List<HeaderFindAndReplace>();

            foreach(var input in fileReRoute.DownstreamHeaderTransform)
            {
                var hAndr = Map(input);
                downstream.Add(hAndr);
            }
            
            return new HeaderTransformations(upstream, downstream);
        }

        private HeaderFindAndReplace Map(KeyValuePair<string,string> input)
        {
            var findAndReplace = input.Value.Split(",");

            var replace = findAndReplace[1].TrimStart();

            var startOfPlaceholder = replace.IndexOf("{");
            if(startOfPlaceholder > -1)
            {
                var endOfPlaceholder = replace.IndexOf("}", startOfPlaceholder);
                
                var placeholder = replace.Substring(startOfPlaceholder, startOfPlaceholder + (endOfPlaceholder + 1));

                if(_placeholders.ContainsKey(placeholder))
                {
                    var value = _placeholders[placeholder].Invoke();
                    replace = replace.Replace(placeholder, value);
                }
            }

            var hAndr = new HeaderFindAndReplace(input.Key, findAndReplace[0], replace, 0);
            
            return hAndr;
        }
    }
}
