using System.Collections.Generic;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class HeaderFindAndReplaceCreator : IHeaderFindAndReplaceCreator
    {
        public HeaderTransformations Create(FileReRoute fileReRoute)
        {
            var upstream = new List<HeaderFindAndReplace>();

            foreach(var input in fileReRoute.UpstreamHeaderTransform)
            {
                var findAndReplace = input.Value.Split(",");
                var hAndr = new HeaderFindAndReplace(input.Key, findAndReplace[0], findAndReplace[1].TrimStart(), 0);
                upstream.Add(hAndr);
            }

            var downstream = new List<HeaderFindAndReplace>();

            foreach(var input in fileReRoute.DownstreamHeaderTransform)
            {
                var findAndReplace = input.Value.Split(",");
                var hAndr = new HeaderFindAndReplace(input.Key, findAndReplace[0], findAndReplace[1].TrimStart(), 0);
                downstream.Add(hAndr);
            }
            
            return new HeaderTransformations(upstream, downstream);
        }
    }
}
