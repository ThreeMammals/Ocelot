using Consul;
using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Consul.Interfaces;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.Consul;

public sealed class DefaultConsulServiceBuilderTests
{
    private DefaultConsulServiceBuilder sut;
    private readonly Mock<IHttpContextAccessor> contextAccessor;
    private readonly Mock<IConsulClientFactory> clientFactory;
    private readonly Mock<IOcelotLoggerFactory> loggerFactory;
    private readonly Mock<IOcelotLogger> logger;
    private ConsulRegistryConfiguration _configuration;

    public DefaultConsulServiceBuilderTests()
    {
        contextAccessor = new();
        clientFactory = new();
        clientFactory.Setup(x => x.Get(It.IsAny<ConsulRegistryConfiguration>()))
            .Returns(new ConsulClient());
        logger = new();
        loggerFactory = new();
        loggerFactory.Setup(x => x.CreateLogger<DefaultConsulServiceBuilder>())
            .Returns(logger.Object);
    }

    private void Arrange([CallerMemberName] string testName = null)
    {
        _configuration = new(null, null, 0, testName, null);
        var context = new DefaultHttpContext();
        context.Items.Add(nameof(ConsulRegistryConfiguration), _configuration);
        contextAccessor.SetupGet(x => x.HttpContext).Returns(context);
        sut = new DefaultConsulServiceBuilder(contextAccessor.Object, clientFactory.Object, loggerFactory.Object);
    }

    [Fact]
    public void Ctor_PrivateMembers_PropertiesAreInitialized()
    {
        Arrange();
        var propClient = sut.GetType().GetProperty("Client", BindingFlags.NonPublic | BindingFlags.Instance);
        var propLogger = sut.GetType().GetProperty("Logger", BindingFlags.NonPublic | BindingFlags.Instance);
        var propConfiguration = sut.GetType().GetProperty("Configuration", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        //var actualConfiguration = sut.Configuration;
        var actualConfiguration = propConfiguration.GetValue(sut);
        var actualClient = propClient.GetValue(sut);
        var actualLogger = propLogger.GetValue(sut);

        // Assert
        actualConfiguration.ShouldNotBeNull().ShouldBe(_configuration);
        actualClient.ShouldNotBeNull();
        actualLogger.ShouldNotBeNull();
    }

    private static Type Me { get; } = typeof(DefaultConsulServiceBuilder);
    private static MethodInfo GetNode { get; } = Me.GetMethod("GetNode", BindingFlags.NonPublic | BindingFlags.Instance);

    [Fact]
    public void GetNode_EntryBranch_ReturnsEntryNode()
    {
        Arrange();
        Node node = new() { Name = nameof(GetNode_EntryBranch_ReturnsEntryNode) };
        ServiceEntry entry = new() { Node = node };

        // Act
        var actual = GetNode.Invoke(sut, new object[] { entry, null }) as Node;

        // Assert
        actual.ShouldNotBeNull().ShouldBe(node);
        actual.Name.ShouldBe(node.Name);
    }

    [Fact]
    public void GetNode_NodesBranch_ReturnsNodeFromCollection()
    {
        Arrange();
        ServiceEntry entry = new()
        {
            Node = null,
            Service = new() { Address = nameof(GetNode_NodesBranch_ReturnsNodeFromCollection) },
        };
        Node[] nodes = null;

        // Act, Assert: nodes is null
        var actual = GetNode.Invoke(sut, new object[] { entry, nodes }) as Node;
        actual.ShouldBeNull();

        // Arrange, Act, Assert: nodes has items, happy path
        var node = new Node { Address = nameof(GetNode_NodesBranch_ReturnsNodeFromCollection) };
        nodes = new[] { node };
        actual = GetNode.Invoke(sut, new object[] { entry, nodes }) as Node;
        actual.ShouldNotBeNull().ShouldBe(node);
        actual.Address.ShouldBe(entry.Service.Address);

        // Arrange, Act, Assert: nodes has items, some nulls in entry
        entry.Service.Address = null;
        actual = GetNode.Invoke(sut, new object[] { entry, nodes }) as Node;
        actual.ShouldBeNull();

        entry.Service = null;
        actual = GetNode.Invoke(sut, new object[] { entry, nodes }) as Node;
        actual.ShouldBeNull();

        entry = null;
        actual = GetNode.Invoke(sut, new object[] { entry, nodes }) as Node;
        actual.ShouldBeNull();
    }

    private static MethodInfo GetDownstreamHost { get; } = Me.GetMethod("GetDownstreamHost", BindingFlags.NonPublic | BindingFlags.Instance);

    [Fact]
    public void GetDownstreamHost_BothBranches_NameOrAddress()
    {
        Arrange();

        // Arrange, Act, Assert: node branch
        ServiceEntry entry = new()
        {
            Service = new() { Address = nameof(GetDownstreamHost_BothBranches_NameOrAddress) },
        };
        var node = new Node { Name = "test1" };
        var actual = GetDownstreamHost.Invoke(sut, new object[] { entry, node }) as string;
        actual.ShouldNotBeNull().ShouldBe("test1");

        // Arrange, Act, Assert: entry branch
        node = null;
        actual = GetDownstreamHost.Invoke(sut, new object[] { entry, node }) as string;
        actual.ShouldNotBeNull().ShouldBe(nameof(GetDownstreamHost_BothBranches_NameOrAddress));
    }

    private static MethodInfo GetServiceVersion { get; } = Me.GetMethod("GetServiceVersion", BindingFlags.NonPublic | BindingFlags.Instance);

    [Fact]
    public void GetServiceVersion_TagsIsNull_EmptyString()
    {
        Arrange();

        // Arrange, Act, Assert: collection is null
        ServiceEntry entry = new()
        {
            Service = new() { Tags = null },
        };
        Node node = null;
        var actual = GetServiceVersion.Invoke(sut, new object[] { entry, node }) as string;
        actual.ShouldBe(string.Empty);

        // Arrange, Act, Assert: collection has no version tag
        entry.Service.Tags = new[] { "test" };
        actual = GetServiceVersion.Invoke(sut, new object[] { entry, node }) as string;
        actual.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetServiceVersion_HasTags_HappyPath()
    {
        Arrange();

        // Arrange
        var tags = new string[] { "test", "version-v2" };
        ServiceEntry entry = new()
        {
            Service = new() { Tags = tags },
        };
        Node node = null;

        // Act
        var actual = GetServiceVersion.Invoke(sut, new object[] { entry, node }) as string;

        // Assert
        actual.ShouldBe("v2");
    }

    private static MethodInfo GetServiceTags { get; } = Me.GetMethod("GetServiceTags", BindingFlags.NonPublic | BindingFlags.Instance);

    [Fact]
    public void GetServiceTags_BothBranches()
    {
        Arrange();

        // Arrange, Act, Assert: collection is null
        ServiceEntry entry = new()
        {
            Service = new() { Tags = null },
        };
        Node node = null;
        var actual = GetServiceTags.Invoke(sut, new object[] { entry, node }) as IEnumerable<string>;
        actual.ShouldNotBeNull().ShouldBeEmpty();

        // Arrange, Act, Assert: happy path
        entry.Service.Tags = new string[] { "1", "2", "3" };
        actual = GetServiceTags.Invoke(sut, new object[] { entry, node }) as IEnumerable<string>;
        actual.ShouldNotBeNull().ShouldNotBeEmpty();
        actual.Count().ShouldBe(3);
        actual.ShouldContain("3");
    }
}
