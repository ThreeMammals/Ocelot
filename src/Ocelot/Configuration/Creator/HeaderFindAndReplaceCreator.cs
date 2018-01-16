using System.Collections.Generic;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class HeaderFindAndReplaceCreator : IHeaderFindAndReplaceCreator
    {
        public List<HeaderFindAndReplace> Create(FileReRoute fileReRoute)
        {
            var headerFindAndReplace = new List<HeaderFindAndReplace>();

            foreach(var input in fileReRoute.UpstreamHeaderTransform)
            {
                var findAndReplace = input.Value.Split(",");
                var hAndr = new HeaderFindAndReplace(input.Key, findAndReplace[0], findAndReplace[1].TrimStart(), 0);
                headerFindAndReplace.Add(hAndr);
            }
            
            return headerFindAndReplace;
        }
    }
}
