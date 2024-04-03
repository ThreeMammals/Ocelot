namespace Ocelot.Configuration.Creator;

public interface IVersionPolicyCreator
{
    HttpVersionPolicy Create(string downstreamVersionPolicy);
}
