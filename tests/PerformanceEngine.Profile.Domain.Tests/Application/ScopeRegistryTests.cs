using FluentAssertions;
using PerformanceEngine.Profile.Domain.Application.Services;
using PerformanceEngine.Profile.Domain.Domain.Scopes;

namespace PerformanceEngine.Profile.Domain.Tests.Application;

/// <summary>
/// Tests for ScopeRegistry runtime registration.
/// </summary>
public class ScopeRegistryTests
{
    [Fact]
    public void RegisterScope_ValidType_Success()
    {
        // Arrange
        var registry = new ScopeRegistry();
        var scopeType = "CustomType";

        // Act
        registry.RegisterScope(scopeType, () => new ApiScope("test"));

        // Assert
        registry.IsRegistered(scopeType).Should().BeTrue();
    }

    [Fact]
    public void GetScopeByType_RegisteredType_ReturnsScope()
    {
        // Arrange
        var registry = new ScopeRegistry();
        var scopeType = "ApiScope";
        var expectedScope = new ApiScope("payment");

        registry.RegisterScope(scopeType, () => expectedScope);

        // Act
        var result = registry.GetScopeByType(scopeType);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedScope);
    }

    [Fact]
    public void GetScopeByType_UnregisteredType_ReturnsNull()
    {
        // Arrange
        var registry = new ScopeRegistry();

        // Act
        var result = registry.GetScopeByType("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void RegisterScope_DuplicateType_ThrowsException()
    {
        // Arrange
        var registry = new ScopeRegistry();
        var scopeType = "CustomType";

        registry.RegisterScope(scopeType, () => new ApiScope("test1"));

        // Act
        Action act = () => registry.RegisterScope(scopeType, () => new ApiScope("test2"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public void UnregisterScope_RegisteredType_Success()
    {
        // Arrange
        var registry = new ScopeRegistry();
        var scopeType = "CustomType";

        registry.RegisterScope(scopeType, () => new ApiScope("test"));

        // Act
        var result = registry.UnregisterScope(scopeType);

        // Assert
        result.Should().BeTrue();
        registry.IsRegistered(scopeType).Should().BeFalse();
    }

    [Fact]
    public void GetRegisteredTypes_ReturnsAllTypes()
    {
        // Arrange
        var registry = new ScopeRegistry();

        registry.RegisterScope("Type1", () => new ApiScope("test1"));
        registry.RegisterScope("Type2", () => new ApiScope("test2"));
        registry.RegisterScope("Type3", () => new ApiScope("test3"));

        // Act
        var types = registry.GetRegisteredTypes();

        // Assert
        types.Should().HaveCount(3);
        types.Should().Contain(new[] { "Type1", "Type2", "Type3" });
    }

    [Fact]
    public void Clear_RemovesAllRegistrations()
    {
        // Arrange
        var registry = new ScopeRegistry();

        registry.RegisterScope("Type1", () => new ApiScope("test1"));
        registry.RegisterScope("Type2", () => new ApiScope("test2"));

        // Act
        registry.Clear();

        // Assert
        registry.GetRegisteredTypes().Should().BeEmpty();
    }

    [Fact]
    public void RegisterScope_NullFactory_ThrowsException()
    {
        // Arrange
        var registry = new ScopeRegistry();

        // Act
        Action act = () => registry.RegisterScope("CustomType", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterScope_EmptyType_ThrowsException()
    {
        // Arrange
        var registry = new ScopeRegistry();

        // Act
        Action act = () => registry.RegisterScope(string.Empty, () => new ApiScope("test"));

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
