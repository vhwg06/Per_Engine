namespace PerformanceEngine.Application.Orchestration;

using PerformanceEngine.Application.Models;
using PerformanceEngine.Application.Ports;
using PerformanceEngine.Application.Services;
using PerformanceEngine.Profile.Domain.Domain.Profiles;

/// <summary>
/// Main entry point for the Evaluate Performance use case.
/// Orchestrates the end-to-end evaluation flow from metrics to final result.
/// </summary>
public sealed class EvaluatePerformanceUseCase
{
    private readonly IMetricsProvider _metricsProvider;
    private readonly IProfileResolver _profileResolver;
    private readonly IEvaluationRulesProvider _rulesProvider;
    private readonly CompletenessAssessor _completenessAssessor;
    private readonly RuleEvaluationCoordinator _ruleCoordinator;
    private readonly OutcomeAggregator _outcomeAggregator;
    private readonly ResultConstructor _resultConstructor;
    private readonly DeterministicFingerprintGenerator _fingerprintGenerator;

    /// <summary>
    /// Initializes a new instance of the EvaluatePerformanceUseCase.
    /// </summary>
    public EvaluatePerformanceUseCase(
        IMetricsProvider metricsProvider,
        IProfileResolver profileResolver,
        IEvaluationRulesProvider rulesProvider)
    {
        _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));
        _profileResolver = profileResolver ?? throw new ArgumentNullException(nameof(profileResolver));
        _rulesProvider = rulesProvider ?? throw new ArgumentNullException(nameof(rulesProvider));

        // Initialize orchestration components
        _completenessAssessor = new CompletenessAssessor();
        _ruleCoordinator = new RuleEvaluationCoordinator();
        _outcomeAggregator = new OutcomeAggregator();
        _resultConstructor = new ResultConstructor();
        _fingerprintGenerator = new DeterministicFingerprintGenerator();
    }

    /// <summary>
    /// Executes the performance evaluation orchestration.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to apply.</param>
    /// <param name="executionContext">Execution context for traceability.</param>
    /// <returns>Immutable evaluation result with outcome, violations, and metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when profile not found or rules collection empty.</exception>
    public EvaluationResult Execute(string profileId, ExecutionContext executionContext)
    {
        // Step 1: Validate inputs (fail-fast)
        ValidateInputs(profileId);

        // Step 2: Resolve profile
        var profile = ResolveProfile(profileId);

        // Step 3: Get evaluation rules
        var rules = GetRules();

        // Step 4: Get available metrics
        var samples = _metricsProvider.GetAvailableSamples();

        // Step 5: Assess completeness
        var completenessReport = _completenessAssessor.AssessCompleteness(rules, samples, _metricsProvider);

        // Step 6: Evaluate rules (deterministic order)
        var violations = _ruleCoordinator.EvaluateRules(rules, samples, _rulesProvider);

        // Step 7: Aggregate outcome
        var outcome = _outcomeAggregator.DetermineOutcome(violations, completenessReport);

        // Step 8: Generate fingerprint
        var fingerprint = _fingerprintGenerator.GenerateFingerprint(samples);

        // Step 9: Construct immutable result
        var evaluationTimestamp = DateTime.UtcNow;
        var result = _resultConstructor.ConstructResult(
            outcome,
            violations,
            completenessReport,
            profile,
            executionContext,
            fingerprint,
            evaluationTimestamp,
            rules.Count);

        return result;
    }

    private void ValidateInputs(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            throw new ArgumentException("Profile ID cannot be null or empty", nameof(profileId));
        }

        if (!_profileResolver.ProfileExists(profileId))
        {
            var availableProfiles = string.Join(", ", _profileResolver.GetAvailableProfileIds());
            throw new ArgumentException(
                $"Profile '{profileId}' not found in available profiles: [{availableProfiles}]",
                nameof(profileId));
        }

        var rules = _rulesProvider.GetRules();
        if (rules == null || rules.Count == 0)
        {
            throw new ArgumentException("No evaluation rules provided; cannot evaluate");
        }
    }

    private ResolvedProfile ResolveProfile(string profileId)
    {
        try
        {
            return _profileResolver.ResolveProfile(profileId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to resolve profile '{profileId}': {ex.Message}",
                ex);
        }
    }

    private IReadOnlyCollection<EvaluationRuleDefinition> GetRules()
    {
        var rules = _rulesProvider.GetRules();
        if (rules == null || rules.Count == 0)
        {
            throw new InvalidOperationException("No evaluation rules available");
        }
        return rules;
    }
}
