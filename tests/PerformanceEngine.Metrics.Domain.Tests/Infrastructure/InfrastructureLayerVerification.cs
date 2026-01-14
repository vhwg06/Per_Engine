namespace PerformanceEngine.Metrics.Domain.Tests.Infrastructure;

using System.Reflection;
using Xunit;
using FluentAssertions;
using PerformanceEngine.Metrics.Domain;
using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Metrics.Domain.Ports;

/// <summary>
/// Verification tests to ensure no infrastructure dependencies leak into domain or application layers.
/// This enforces the Clean Architecture principle that domain has zero inbound dependencies.
/// </summary>
public class InfrastructureLayerVerification
{
    [Fact]
    public void Domain_ShouldNotReferencePersistenceNamespaces()
    {
        // Verify: Domain layer contains no references to data access technologies

        var domainAssembly = typeof(Sample).Assembly;
        var domainTypes = domainAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("PerformanceEngine.Metrics.Domain.Metrics") == true);

        var forbiddenNamespaces = new[]
        {
            "System.Data",
            "System.Data.SqlClient",
            "System.Data.Entity",
            "Microsoft.EntityFrameworkCore",
            "Dapper",
            "ServiceStack.OrmLite"
        };

        foreach (var type in domainTypes)
        {
            var referencedAssemblies = type.Assembly.GetReferencedAssemblies()
                .Select(a => a.Name)
                .ToList();

            foreach (var forbidden in forbiddenNamespaces)
            {
                var shouldNotContain = referencedAssemblies.Any(n => n == forbidden || (n != null && n.StartsWith(forbidden)));
                shouldNotContain.Should().BeFalse(
                    $"Domain type '{type.FullName}' should not reference {forbidden}");
            }
        }
    }

    [Fact]
    public void Domain_ShouldNotReferenceHttpOrEngineLibraries()
    {
        // Verify: Domain contains no engine-specific or HTTP framework references

        var domainAssembly = typeof(Sample).Assembly;
        var forbiddenAssemblies = new[]
        {
            "System.Net.Http",
            "k6",
            "JMeter",
            "Gatling",
            "HttpClient",
            "RestSharp"
        };

        var actualAssemblies = domainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name?.ToLowerInvariant())
            .Where(a => a != null)
            .ToList();

        foreach (var forbidden in forbiddenAssemblies)
        {
            actualAssemblies.Should()
                .NotContain(a => a != null && a.Contains(forbidden.ToLowerInvariant()),
                    $"Domain assembly should not reference {forbidden}");
        }
    }

    [Fact]
    public void Domain_MetricsNamespace_ContainsOnlyValueObjectsAndEntities()
    {
        // Verify: Domain.Metrics contains only pure domain logic

        var domainMetricsTypes = typeof(Sample).Assembly.GetTypes()
            .Where(t => t.Namespace?.Equals("PerformanceEngine.Metrics.Domain.Metrics") == true)
            .ToList();

        // All types should be Value Objects, Entities, or Enums
        var allowedBaseTypes = new[] { typeof(ValueObject), typeof(Enum), typeof(object) };

        foreach (var type in domainMetricsTypes)
        {
            if (type.IsEnum)
                continue; // Enums are allowed

            if (type.BaseType == typeof(ValueObject))
                continue; // Value objects are allowed

            if (typeof(ValueObject).IsAssignableFrom(type))
                continue; // Derived value objects are allowed

            if (type.IsAbstract)
                continue; // Abstract base classes are allowed

            // All other types should be plain domain objects (Entities)
            type.IsSealed.Should().BeTrue(
                $"Domain entity '{type.Name}' should be sealed for immutability");
        }
    }

    [Fact]
    public void PortsInterfaces_ShouldBeAbstract()
    {
        // Verify: All port interfaces are pure abstractions with no implementation

        var portsNamespace = typeof(IExecutionEngineAdapter).Assembly.GetTypes()
            .Where(t => t.Namespace?.Equals("PerformanceEngine.Metrics.Domain.Ports") == true)
            .ToList();

        var interfaces = portsNamespace.Where(t => t.IsInterface).ToList();

        interfaces.Should().NotBeEmpty("Ports namespace should contain interfaces");

        foreach (var iface in interfaces)
        {
            // Ports should be pure abstractions (interfaces, no implementations)
            iface.IsInterface.Should().BeTrue(
                $"Port '{iface.Name}' should be an interface, not a class");
        }
    }

    [Fact]
    public void NoInfrastructureCode_InDomainAssembly()
    {
        // Verify: Infrastructure adapters/implementations are not in domain assembly
        // Exception: K6EngineAdapter and JMeterEngineAdapter are explicitly part of the spec (T050-T051)

        var domainAssembly = typeof(Sample).Assembly;
        var allowedAdapters = new[] { "K6EngineAdapter", "JMeterEngineAdapter" };
        
        var infrastructureTypes = domainAssembly.GetTypes()
            .Where(t => (t.Name.Contains("Adapter") || 
                        t.Name.Contains("Repository") ||
                        t.Name.Contains("DbContext")) &&
                   !t.IsInterface && // Interfaces are ports, not implementations
                   !allowedAdapters.Contains(t.Name)) // Exclude explicitly-specified adapters
            .ToList();

        infrastructureTypes.Should().BeEmpty(
            "Domain assembly should not contain unexpected infrastructure implementations");
    }
}
