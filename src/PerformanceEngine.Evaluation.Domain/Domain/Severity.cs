namespace PerformanceEngine.Evaluation.Domain;

/// <summary>
/// Outcome severity levels ordered from least to most severe.
/// Used to classify evaluation results and violations.
/// </summary>
public enum Severity
{
    /// <summary>All rules satisfied - test passed</summary>
    Pass = 0,

    /// <summary>Minor violations detected - test still acceptable</summary>
    Warning = 1,

    /// <summary>Critical violations detected - test failed</summary>
    Fail = 2
}
