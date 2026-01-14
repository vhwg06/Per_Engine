namespace PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Factory for creating built-in scope types.
/// Custom scopes can be instantiated directly by application code.
/// </summary>
public static class ScopeFactory
{
    /// <summary>
    /// Creates a global scope.
    /// </summary>
    public static IScope CreateGlobal() => GlobalScope.Instance;

    /// <summary>
    /// Creates an API-specific scope.
    /// </summary>
    public static IScope CreateApi(string apiName) => new ApiScope(apiName);

    /// <summary>
    /// Creates an environment-specific scope.
    /// </summary>
    public static IScope CreateEnvironment(string environmentName) =>
        new EnvironmentScope(environmentName);

    /// <summary>
    /// Creates a tag-specific scope.
    /// </summary>
    public static IScope CreateTag(string tagName, int precedence = 20) =>
        new TagScope(tagName, precedence);

    /// <summary>
    /// Creates a composite scope from two base scopes.
    /// </summary>
    public static IScope CreateComposite(IScope scopeA, IScope scopeB) =>
        new CompositeScope(scopeA, scopeB);

    /// <summary>
    /// Creates a composite scope for an API in a specific environment.
    /// </summary>
    public static IScope CreateApiEnvironment(string apiName, string environmentName) =>
        new CompositeScope(new ApiScope(apiName), new EnvironmentScope(environmentName));
}
