using GraphQL;

namespace Ocelot.Samples.GraphQL.Models;

public class Query
{
    private readonly List<Hero> _heroes = new()
    {
        new Hero { Id = 1, Name = "R2-D2" },
        new Hero { Id = 2, Name = "Batman" },
        new Hero { Id = 3, Name = "Wonder Woman" },
        new Hero { Id = 4, Name = "Tom Pallister" }
    };

    [GraphQLMetadata("hero")]
    public Hero? GetHero(int id)
    {
        return _heroes.FirstOrDefault(x => x.Id == id);
    }
}
