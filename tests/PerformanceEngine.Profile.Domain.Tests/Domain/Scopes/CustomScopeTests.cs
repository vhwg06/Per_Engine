using FluentAssertions;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using System.Collections.Immutable;

namespace PerformanceEngine.Profile.Domain.Tests.Domain.Scopes;

/// <summary>
/// Demonstrates custom scope extensibility by implementing a PaymentMethodScope.
/// This example shows how to create domain-specific scopes without modifying core resolver.
/// </summary>
public class CustomScopeTests
{
    /// <summary>
    /// Example custom scope for payment method-specific configuration.
    /// Implements IScope to integrate with ProfileResolver.
    /// </summary>
    public record PaymentMethodScope : IScope
    {
        public string PaymentMethod { get; init; }

        public PaymentMethodScope(string paymentMethod, int precedence = 18)
        {
            PaymentMethod = paymentMethod ?? throw new ArgumentNullException(nameof(paymentMethod));
            Precedence = precedence;
        }

        public string Id => $"payment-method:{PaymentMethod}";
        public string Type => "PaymentMethod";
        public int Precedence { get; init; }
        public string Description => $"Payment method: {PaymentMethod}";

        public int CompareTo(IScope? other)
        {
            if (other == null) return 1;
            return Precedence.CompareTo(other.Precedence);
        }

        public bool Equals(IScope? other)
        {
            return other is PaymentMethodScope pms &&
                   pms.PaymentMethod == PaymentMethod &&
                   pms.Precedence == Precedence;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PaymentMethod, Precedence);
        }
    }

    [Fact]
    public void CustomScope_IntegratesWithResolver()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3));

        var visaConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("45s"))
            .Add(new ConfigKey("fraud_check"), ConfigValue.Create(true));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(new PaymentMethodScope("visa"), visaConfig)
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, new PaymentMethodScope("visa"));

        // Assert - Custom scope overrides global
        result.Configuration.Should().HaveCount(3);
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("45s");
        result.Configuration[new ConfigKey("retries")].Value.Should().Be(3);
        result.Configuration[new ConfigKey("fraud_check")].Value.Should().Be(true);
    }

    [Fact]
    public void CustomScope_RespectsHierarchy()
    {
        // Arrange - Environment (15) should beat PaymentMethod (18 > 15)
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var envConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var paymentConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("90s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(new EnvironmentScope("prod"), envConfig),
            new ProfileEntity(new PaymentMethodScope("visa", precedence: 18), paymentConfig)
        };

        var requestedScopes = new IScope[]
        {
            new EnvironmentScope("prod"),
            new PaymentMethodScope("visa", precedence: 18)
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert - PaymentMethod (18) beats Environment (15)
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("90s");
    }

    [Fact]
    public void CustomScope_WorksWithCompositeScope()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var apiScope = new ApiScope("checkout");
        var paymentScope = new PaymentMethodScope("mastercard", precedence: 18);
        var compositeScope = new CompositeScope(apiScope, paymentScope);

        var compositeConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("120s"))
            .Add(new ConfigKey("3ds_required"), ConfigValue.Create(true));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(compositeScope, compositeConfig)
        };

        var requestedScopes = new IScope[] { apiScope, paymentScope };

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert - Composite scope applies
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("120s");
        result.Configuration[new ConfigKey("3ds_required")].Value.Should().Be(true);
    }

    [Fact]
    public void CustomScope_SupportsMultipleInstances()
    {
        // Arrange - Different payment methods
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var visaConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("40s"));

        var mastercardConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("50s"));

        var paypalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(new PaymentMethodScope("visa"), visaConfig),
            new ProfileEntity(new PaymentMethodScope("mastercard"), mastercardConfig),
            new ProfileEntity(new PaymentMethodScope("paypal"), paypalConfig)
        };

        // Act - Resolve for Mastercard
        var result = ProfileResolver.Resolve(profiles, new PaymentMethodScope("mastercard"));

        // Assert - Only Mastercard config applies
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("50s");
    }

    [Fact]
    public void CustomScope_EqualityWorks()
    {
        // Arrange
        var scope1 = new PaymentMethodScope("visa", precedence: 18);
        var scope2 = new PaymentMethodScope("visa", precedence: 18);
        var scope3 = new PaymentMethodScope("mastercard", precedence: 18);

        // Act & Assert
        scope1.Equals(scope2).Should().BeTrue();
        scope1.Equals(scope3).Should().BeFalse();
        scope1.GetHashCode().Should().Be(scope2.GetHashCode());
    }

    [Fact]
    public void CustomScope_ComparisonWorks()
    {
        // Arrange
        var lowerPrecedence = new PaymentMethodScope("visa", precedence: 10);
        var higherPrecedence = new PaymentMethodScope("mastercard", precedence: 20);

        // Act
        var comparison = lowerPrecedence.CompareTo(higherPrecedence);

        // Assert
        comparison.Should().BeLessThan(0); // Lower precedence comes first
    }

    [Fact]
    public void CustomScope_WithDifferentPaymentMethods_OnlyMatchingApplies()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var visaConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(new PaymentMethodScope("visa"), visaConfig)
        };

        // Act - Request Mastercard, but only Visa profile exists
        var result = ProfileResolver.Resolve(profiles, new PaymentMethodScope("mastercard"));

        // Assert - Visa config should NOT apply (different payment method)
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("30s"); // Global wins
    }
}
