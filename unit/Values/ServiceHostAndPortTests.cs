using Ocelot.Values;

namespace Ocelot.UnitTests.Values;

public class ServiceHostAndPortTests : UnitTest
{
    [Fact]
    public void CopyCtor_WhenCalled_ThenCopiesAllProperties()
    {
        // Arrange
        var original = new ServiceHostAndPort("h", 1, "s");

        // Act
        var copy = new ServiceHostAndPort(original);

        // Assert
        Assert.Equal(original.DownstreamHost, copy.DownstreamHost);
        Assert.Equal(original.DownstreamPort, copy.DownstreamPort);
        Assert.Equal(original.Scheme, copy.Scheme);
        Assert.Equal(original.GetHashCode(), copy.GetHashCode());
        Assert.True(original.Equals(copy));
        Assert.True(original == copy);
    }

    [Theory]
    [InlineData(null, 8)]
    [InlineData("ocelot.net", 80)]
    [InlineData("ocelot.net/", 443)]
    public void Ctor_string_int_WhenCalled_ThenPropertiesSetAndToStringContainsEmptyScheme(string host, int port)
    {
        // Arrange

        // Act
        var sh = new ServiceHostAndPort(host, port);

        // Assert
        if (host is null)
            Assert.Null(sh.DownstreamHost);
        else
            Assert.Equal("ocelot.net", sh.DownstreamHost);
        Assert.Equal(port, sh.DownstreamPort);
        Assert.Null(sh.Scheme);
        Assert.Equal($":{sh.DownstreamHost}:{sh.DownstreamPort}", sh.ToString());
    }

    [Fact]
    public void Ctor_string_int_string_WhenCalled_ThenSchemeSetAndToStringIncludesScheme()
    {
        // Arrange
        var host = "api";
        var port = 123;
        var scheme = "https";

        // Act
        var sh = new ServiceHostAndPort(host, port, scheme);

        // Assert
        Assert.Equal("api", sh.DownstreamHost);
        Assert.Equal(123, sh.DownstreamPort);
        Assert.Equal("https", sh.Scheme);
        Assert.Equal($"{scheme}:{host}:{port}", sh.ToString());
    }

    [Fact]
    public void ToString_()
    {
        // Arrange
        var sh = new ServiceHostAndPort("ocelot.net", 123, "https");

        // Act
        var actual = sh.ToString();

        // Assert
        Assert.Equal("https:ocelot.net:123", actual);
    }

    [Fact]
    public void GetHashCode_WhenNullOrDefaultValues_ThenConstant()
    {
        // Arrange
        var a = new ServiceHostAndPort(null, 0);
        var b = new ServiceHostAndPort(null, 0);

        // Act
        int ha1 = a.GetHashCode();
        int ha2 = a.GetHashCode();
        int hb = b.GetHashCode();

        // Assert
        Assert.Equal(ha1, ha2);
        Assert.Equal(ha1, hb);
    }

    [Fact]
    public void GetHashCode_WhenSameProperties_ThenSameHashCode()
    {
        // Arrange
        var x = new ServiceHostAndPort("host", 11, "http");
        var y = new ServiceHostAndPort("host", 11, "http");

        // Act
        int hx = x.GetHashCode();
        int hy = y.GetHashCode();

        // Assert
        Assert.Equal(hx, hy);
    }

    [Fact]
    public void GetHashCode_WhenDifferentProperties_ThenDifferentHashCodes()
    {
        // Arrange
        var x = new ServiceHostAndPort("host1", 11, "http");
        var y = new ServiceHostAndPort("host2", 12, "https");

        // Act
        int hx = x.GetHashCode();
        int hy = y.GetHashCode();

        // Assert
        Assert.NotEqual(hx, hy);
    }

    [Fact]
    public void Equals_ServiceHostAndPort_BasedOnEquality()
    {
        // Arrange
        var a = new ServiceHostAndPort("a", 1);
        var b = new ServiceHostAndPort("b", 1);

        // Act, Assert
        Assert.False(a.Equals(b));
        b = new ServiceHostAndPort("a", 1);
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_Object_BasedOnEquality()
    {
        // Arrange
        var a = new ServiceHostAndPort("a", 1);

        // Act, Assert
        object obj = null;
        Assert.False(a.Equals(obj));

        obj = "not a host";
        Assert.False(a.Equals(obj));

        obj = new ServiceHostAndPort("a", 1);
        Assert.True(a.Equals(obj));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Operator_Equality_WhenNullArguments_BasedOnObjectEquals(bool nullA, bool nullB)
    {
        // Arrange
        ServiceHostAndPort instance = new("a", 1, "s");
        ServiceHostAndPort a = nullA ? null : instance;
        ServiceHostAndPort b = nullB ? null : instance;

        // Act, Assert
        bool expected = Equals(a, b);
        Assert.Equal(expected, a == b);
    }

    [Fact]
    public void Operator_Equality_WhenArgumentsNotNull_PropertiesCompared()
    {
        // Arrange
        ServiceHostAndPort a = new("a", 1, "s");
        ServiceHostAndPort b = new("a", 1, "s");

        // Act, Assert
        Assert.True(a == b);

        b = new("b", 1, "s");
        Assert.False(a == b);

        b = new("a", 2, "s");
        Assert.False(a == b);

        b = new("a", 1, "x");
        Assert.False(a == b);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Operator_Inequality_WhenNullArguments_BasedOnObjectEquals(bool nullA, bool nullB)
    {
        // Arrange
        ServiceHostAndPort instance = new("a", 1, "s");
        ServiceHostAndPort a = nullA ? null : instance;
        ServiceHostAndPort b = nullB ? null : instance;

        // Act, Assert
        bool expected = !Equals(a, b);
        Assert.Equal(expected, a != b);
    }

    [Fact]
    public void Operator_Inequality_WhenArgumentsNotNull_PropertiesCompared()
    {
        // Arrange
        ServiceHostAndPort a = new("a", 1, "s");
        ServiceHostAndPort b = new("a", 1, "s");

        // Act, Assert
        Assert.False(a != b);

        b = new("b", 1, "s");
        Assert.True(a != b);

        b = new("a", 2, "s");
        Assert.True(a != b);

        b = new("a", 1, "x");
        Assert.True(a != b);
    }
}
