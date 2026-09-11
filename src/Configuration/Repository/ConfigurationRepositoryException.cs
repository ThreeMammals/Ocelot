using Ocelot.Errors;

namespace Ocelot.Configuration.Repository;

public class ConfigurationRepositoryException : Exception
{
    public ConfigurationRepositoryException(string message)
        : base(message)
    { }

    public ConfigurationRepositoryException(IEnumerable<Error> errors)
        : base(errors.ToErrorString())
    {
        Data.Add(nameof(errors), errors);
    }
}
