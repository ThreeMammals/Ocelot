using Ocelot.Configuration.Builder;

namespace Ocelot.UnitTests.Configuration.Builder;

public class UpstreamPathTemplateBuilderTests
{
    [Fact]
    public void Should_build_upstream_path_template_with_all_properties_set()
    {
        // Arrange
        var builder = new UpstreamPathTemplateBuilder();

        // Act
        var result = builder
            .WithTemplate("/api/products/{productId}")
            .WithPriority(1)
            .WithContainsQueryString(true)
            .WithOriginalValue("/api/products/{productId}?version=1")
            .Build();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/api/products/{productId}", result.Template);
        Assert.Equal(1, result.Priority);
        Assert.True(result.ContainsQueryString);
        Assert.Equal("/api/products/{productId}?version=1", result.OriginalValue);
    }

    [Fact]
    public void Should_build_with_default_values_when_nothing_is_set()
    {
        // Arrange & Act
        var result = new UpstreamPathTemplateBuilder().Build();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Template);
        Assert.Equal(0, result.Priority); // default int value
        Assert.False(result.ContainsQueryString);
        Assert.Null(result.OriginalValue);
    }

    [Fact]
    public void Should_support_fluent_chaining_in_any_order()
    {
        // Arrange & Act
        var result = new UpstreamPathTemplateBuilder()
            .WithPriority(5)
            .WithTemplate("/users/{userId}/orders")
            .WithOriginalValue("/users/123/orders")
            .WithContainsQueryString(false)
            .Build();

        // Assert
        Assert.Equal("/users/{userId}/orders", result.Template);
        Assert.Equal(5, result.Priority);
        Assert.False(result.ContainsQueryString);
        Assert.Equal("/users/123/orders", result.OriginalValue);
    }

    [Theory]
    [InlineData("/api/{id}", 10, true, "/api/42?test=1")]
    [InlineData("/health", 0, false, null)]
    [InlineData("", 999, true, "")]
    public void Should_correctly_set_values_via_theory(string template, int priority, bool containsQueryString, string originalValue)
    {
        // Arrange & Act
        var result = new UpstreamPathTemplateBuilder()
            .WithTemplate(template)
            .WithPriority(priority)
            .WithContainsQueryString(containsQueryString)
            .WithOriginalValue(originalValue)
            .Build();

        // Assert
        Assert.Equal(template, result.Template);
        Assert.Equal(priority, result.Priority);
        Assert.Equal(containsQueryString, result.ContainsQueryString);
        Assert.Equal(originalValue, result.OriginalValue);
    }

    [Fact]
    public void Should_allow_overriding_values_multiple_times()
    {
        // Arrange & Act
        var result = new UpstreamPathTemplateBuilder()
            .WithTemplate("/old")
            .WithPriority(1)
            .WithTemplate("/new/path/{id}")
            .WithPriority(42)
            .WithContainsQueryString(true)
            .WithContainsQueryString(false)
            .Build();

        // Assert
        Assert.Equal("/new/path/{id}", result.Template);
        Assert.Equal(42, result.Priority);
        Assert.False(result.ContainsQueryString);
    }

    [Fact]
    public void Should_create_new_UpstreamPathTemplate_instance_on_each_build()
    {
        // Arrange
        var builder = new UpstreamPathTemplateBuilder()
            .WithTemplate("/api/test")
            .WithPriority(3);

        // Act
        var first = builder.Build();
        var second = builder.Build();

        // Assert
        Assert.NotSame(first, second); // different object instances
        Assert.Equal(first.Template, second.Template);
        Assert.Equal(first.Priority, second.Priority);
    }
}
