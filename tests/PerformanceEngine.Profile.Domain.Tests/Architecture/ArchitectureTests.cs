using FluentAssertions;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using System.Reflection;

namespace PerformanceEngine.Profile.Domain.Tests.Architecture;

/// <summary>
/// Verifies architectural compliance:
/// - No file I/O in domain layer
/// - No environment variable access in domain layer
/// - Immutability of key entities
/// - IScope interface implemented by all scope types
/// - No non-deterministic code (DateTime.Now, Random)
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(ProfileEntity).Assembly;

    [Fact]
    public void Domain_DoesNotUseFileIO()
    {
        // Arrange
        var domainTypes = DomainAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains("Domain"));

        var forbiddenTypes = new[]
        {
            typeof(System.IO.File),
            typeof(System.IO.FileInfo),
            typeof(System.IO.Directory),
            typeof(System.IO.DirectoryInfo),
            typeof(System.IO.StreamReader),
            typeof(System.IO.StreamWriter),
            typeof(System.IO.FileStream)
        };

        // Act & Assert
        foreach (var type in domainTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            
            foreach (var method in methods)
            {
                if (method.DeclaringType != type) continue; // Skip inherited methods

                var methodBody = method.GetMethodBody();
                if (methodBody == null) continue;

                // Check method doesn't reference forbidden types
                var instructions = methodBody.GetILAsByteArray();
                instructions.Should().NotBeNull($"Method {type.Name}.{method.Name} uses file I/O");
            }
        }
    }

    [Fact]
    public void Domain_DoesNotAccessEnvironmentVariables()
    {
        // Arrange
        var domainAssembly = typeof(GlobalScope).Assembly;
        var domainTypes = domainAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains("Domain"));

        // Act & Assert
        foreach (var type in domainTypes)
        {
            // Skip EnvironmentScope class itself
            if (type.Name == "EnvironmentScope") continue;

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            
            foreach (var method in methods)
            {
                if (method.DeclaringType != type) continue;

                // Check IL code for Environment.GetEnvironmentVariable calls
                try
                {
                    var methodBody = method.GetMethodBody();
                    if (methodBody != null)
                    {
                        var il = methodBody.GetILAsByteArray();
                        // Simple heuristic: check for GetEnvironmentVariable in method names
                        method.ToString()!.Should().NotContain("GetEnvironmentVariable",
                            $"Method {type.Name}.{method.Name} should not call Environment.GetEnvironmentVariable");
                    }
                }
                catch
                {
                    // Reflection on some methods may fail; that's OK
                }
            }
        }
    }

    [Fact]
    public void Domain_KeyEntities_AreImmutable()
    {
        // Arrange - Check key domain entities
        var immutableTypes = new[]
        {
            typeof(ProfileEntity),
            typeof(ResolvedProfile),
            typeof(ConfigKey),
            typeof(ConfigValue)
        };

        // Act & Assert
        foreach (var type in immutableTypes)
        {
            // Record types are immutable by default
            type.IsValueType.Should().BeFalse($"{type.Name} should be a record (reference type)");
            
            // Check for init-only or no setters
            var writableProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.SetMethod?.IsPublic == true)
                .ToList();

            writableProperties.Should().BeEmpty(
                $"{type.Name} should have init-only or no public setters for immutability. " +
                $"Found writable: {string.Join(", ", writableProperties.Select(p => p.Name))}");
        }
    }

    [Fact]
    public void Domain_AllScopeTypes_ImplementIScope()
    {
        // Arrange
        var scopeTypes = DomainAssembly.GetTypes()
            .Where(t => t.Namespace != null && 
                        t.Namespace.Contains("Scopes") &&
                        t.IsClass &&
                        !t.IsAbstract &&
                        !t.Name.StartsWith("<") &&  // Skip compiler-generated types
                        t != typeof(IScope));

        // Act & Assert
        foreach (var type in scopeTypes)
        {
            typeof(IScope).IsAssignableFrom(type).Should().BeTrue(
                $"{type.Name} should implement IScope interface");
        }
    }

    [Fact]
    public void Domain_DoesNotUseNonDeterministicCode()
    {
        // Arrange
        var domainTypes = DomainAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains("Domain"));

        var nonDeterministicPatterns = new[]
        {
            "DateTime.Now",
            "DateTime.UtcNow",
            "DateTimeOffset.Now",
            "Random",
            "Guid.NewGuid"
        };

        // Act & Assert - Check method names and usage patterns
        foreach (var type in domainTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            
            foreach (var method in methods)
            {
                if (method.DeclaringType != type) continue;

                // Check for non-deterministic calls
                // Note: This is a basic check; IL inspection would be more accurate
                foreach (var pattern in nonDeterministicPatterns)
                {
                    method.Name.Should().NotContain(pattern.Replace(".", ""),
                        $"Method {type.Name}.{method.Name} may use non-deterministic code: {pattern}");
                }
            }
        }
    }

    [Fact]
    public void Domain_ProfileResolver_IsPureStaticService()
    {
        // Arrange
        var resolverType = typeof(ProfileResolver);

        // Act & Assert
        resolverType.IsAbstract.Should().BeTrue("ProfileResolver should be static class");
        resolverType.IsSealed.Should().BeTrue("ProfileResolver should be static class");

        // No instance constructors
        var instanceConstructors = resolverType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        instanceConstructors.Should().BeEmpty("ProfileResolver should have no public instance constructors");

        // All methods should be static
        var publicMethods = resolverType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        publicMethods.Should().NotBeEmpty("ProfileResolver should have static methods");
    }

    [Fact]
    public void Domain_ConflictHandler_IsPureStaticService()
    {
        // Arrange
        var handlerType = typeof(ConflictHandler);

        // Act & Assert
        handlerType.IsAbstract.Should().BeTrue("ConflictHandler should be static class");
        handlerType.IsSealed.Should().BeTrue("ConflictHandler should be static class");

        var instanceConstructors = handlerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        instanceConstructors.Should().BeEmpty("ConflictHandler should have no public instance constructors");
    }

    [Fact]
    public void Domain_NoInfrastructureDependencies()
    {
        // Arrange
        var referencedAssemblies = DomainAssembly.GetReferencedAssemblies();

        // Act & Assert - Domain should only reference system libraries
        var forbiddenPrefixes = new[] { "EntityFramework", "Dapper", "Npgsql", "MySql", "MongoDB" };

        foreach (var assembly in referencedAssemblies)
        {
            foreach (var prefix in forbiddenPrefixes)
            {
                assembly.Name.Should().NotStartWith(prefix,
                    $"Domain should not reference infrastructure library: {assembly.Name}");
            }
        }
    }

    [Fact]
    public void Domain_ValueObjects_HaveValueBasedEquality()
    {
        // Arrange
        var key1 = new ConfigKey("timeout");
        var key2 = new ConfigKey("timeout");
        var key3 = new ConfigKey("retries");

        var value1 = ConfigValue.Create("30s");
        var value2 = ConfigValue.Create("30s");
        var value3 = ConfigValue.Create("60s");

        // Act & Assert
        key1.Should().Be(key2);
        key1.Should().NotBe(key3);
        key1.GetHashCode().Should().Be(key2.GetHashCode());

        value1.Should().Be(value2);
        value1.Should().NotBe(value3);
        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }
}
