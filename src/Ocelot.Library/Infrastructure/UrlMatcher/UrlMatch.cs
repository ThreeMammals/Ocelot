namespace Ocelot.Library.Infrastructure.UrlMatcher
{
    public class UrlMatch
    {
        public UrlMatch(bool match)
        {
            Match = match; 
        }
        public bool Match {get;private set;}
    }
}