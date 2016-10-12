namespace Ocelot.Library.Infrastructure.Authentication
{
    using Responses;
    public class CouldNotFindConfigurationError : Error
    {
        public CouldNotFindConfigurationError(string message) 
            : base(message)
        {
        }
    }
}
