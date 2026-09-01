namespace MembershipSystem.UseCases;

public enum UseCaseOutcome
{
    Success,
    NotFound,
    ValidationFailed,
}

/// <summary>
/// Outcome of a use case operation that returns a value on success.
/// Kept as a single result type (rather than throwing) so callers —
/// Phase 6 controllers — can map NotFound/ValidationFailed to the right
/// HTTP status without catching exceptions for expected, named outcomes.
/// </summary>
public sealed record UseCaseResult<T>
{
    public UseCaseOutcome Outcome { get; }
    public T? Value { get; }
    public IReadOnlyList<string> Errors { get; }

    private UseCaseResult(UseCaseOutcome outcome, T? value, IReadOnlyList<string> errors)
    {
        Outcome = outcome;
        Value = value;
        Errors = errors;
    }

    public static UseCaseResult<T> Success(T value) => new(UseCaseOutcome.Success, value, []);
    public static UseCaseResult<T> NotFound() => new(UseCaseOutcome.NotFound, default, []);
    public static UseCaseResult<T> ValidationFailed(params string[] errors) =>
        new(UseCaseOutcome.ValidationFailed, default, errors);
}

/// <summary>
/// Outcome of a use case operation with no return value on success
/// (e.g. Remove).
/// </summary>
public sealed record UseCaseResult
{
    public UseCaseOutcome Outcome { get; }
    public IReadOnlyList<string> Errors { get; }

    private UseCaseResult(UseCaseOutcome outcome, IReadOnlyList<string> errors)
    {
        Outcome = outcome;
        Errors = errors;
    }

    public static UseCaseResult Success() => new(UseCaseOutcome.Success, []);
    public static UseCaseResult NotFound() => new(UseCaseOutcome.NotFound, []);
    public static UseCaseResult ValidationFailed(params string[] errors) =>
        new(UseCaseOutcome.ValidationFailed, errors);
}
