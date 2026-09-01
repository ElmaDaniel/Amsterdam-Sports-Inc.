namespace MembershipSystem.UseCases;

public sealed record BranchSummary(Guid Id, string Name);

public sealed record MemberSummary(Guid Id, string FirstName, string LastName, IReadOnlyList<string> SportNames);

public sealed record SportSummary(Guid Id, string Name);

public sealed record MemberDetail(
    Guid Id,
    string FirstName,
    string LastName,
    string? PhotoPath,
    IReadOnlyList<SportSummary> Sports);
