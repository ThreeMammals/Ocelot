using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using static Ocelot.Requester.MessageInvokerPool;

namespace Ocelot.UnitTests.Requester;

public class MessageInvokerCacheKeyTests
{
    [Fact]
    public void Equals_Object()
    {
        var route1 = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new("/r1", 0, false, "/r1"))
            .Build();
        var route2 = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new("/r2", 0, false, "/r2"))
            .Build();
        var key1 = new MessageInvokerCacheKey(route1);
        object key2 = new MessageInvokerCacheKey(route2);

        // Act, Assert 0: If different types
        bool isDiffTypes = key1.Equals(new DownstreamRouteBuilder());
        Assert.False(isDiffTypes);

        // Act, Assert 1
        bool isEqual = key1.Equals(key2);
        Assert.False(isEqual);

        // Arrange 2
        var route3 = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new("/r1", 0, false, "/r3"))
            .Build();
        object key3 = new MessageInvokerCacheKey(route3);

        // Act, Assert 1
        isEqual = key1.Equals(key3);
        Assert.False(isEqual);

        // Arrange 3
        var route4 = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new("/r1", 0, false, "/r1"))
            .Build();
        object key4 = new MessageInvokerCacheKey(route4);

        // Act, Assert 1
        isEqual = key1.Equals(key4);

        // Assert.True(isEqual); // O-ho-ho! :(
        Assert.False(isEqual); // actually objects are different :(

        // Life hack for Guillaume ;)) LoL
        // This method has taken from source code of the public sealed partial class ObjectEqualityComparer<T> : EqualityComparer<T>
        // Link to source: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/EqualityComparer.cs#L186-L198
        static bool EqualsForGui(DownstreamRoute x, DownstreamRoute y)
        {
            if (x != null)
            {
                if (y != null) return x.Equals(y); // object.Equals(object) -> https://github.com/dotnet/runtime/blob/0621e649bd084cb0dfd1f2e627538e7d9aa9e211/src/libraries/System.Private.CoreLib/src/System/Object.cs#L45-L64
                return false;
            }

            if (y != null) return false;
            return true;
        }

        // Two object with absolutely identical internal state
        var d1 = new DownstreamRoute(default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default);
        var d2 = new DownstreamRoute(default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default, default);

        // Act for Gui :)
        bool happyStart = d1.Equals(d2); // object.Equals(object)
        bool happyEnd = EqualsForGui(d1, d2); // also uses object.Equals(object)

        // Assert Bingo!
        Assert.Equal(happyStart, happyEnd);
        Assert.True(EqualsForGui(d1, d1)); // it is true because same reference was compared to itself by object.Equals(object), so this is default implementation of object.Equals(object)

        // Assert.True(happyEnd); // but it is False actually 
        Assert.False(happyEnd); // No happy end by Gui...
    }

    [Fact]
    public void Equality_Operator()
    {
        var route1 = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new("/r1", 0, false, "/r1"))
            .Build();
        var route2 = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new("/r2", 0, false, "/r2"))
            .Build();
        var key1 = new MessageInvokerCacheKey(route1);
        var key2 = new MessageInvokerCacheKey(route2);

        // Act
        bool isEqual = key1 == key2;

        // Assert
        Assert.False(isEqual); 
    }

    [Fact]
    public void Inequality_Operator()
    {
        var route1 = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new("/r1", 0, false, "/r1"))
            .Build();
        var route2 = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new("/r2", 0, false, "/r2"))
            .Build();
        var key1 = new MessageInvokerCacheKey(route1);
        var key2 = new MessageInvokerCacheKey(route2);

        // Act
        bool notEqual = key1 != key2;

        // Assert
        Assert.True(notEqual);
    }
}
