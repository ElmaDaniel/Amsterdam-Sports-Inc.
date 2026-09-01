namespace MembershipSystem.Api.Contracts;

public sealed record MemberListItemResponse(Guid Id, string FirstName, string LastName, IReadOnlyList<string> Sports);

public sealed record SportRefResponse(Guid Id, string Name);

public sealed record MemberDetailResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? PhotoPath,
    IReadOnlyList<SportRefResponse> Sports);

public sealed record CreateMemberRequest(string FirstName, string LastName, IReadOnlyList<Guid>? SportIds);

public sealed record UpdateMemberRequest(string FirstName, string LastName, IReadOnlyList<Guid>? SportIds);
