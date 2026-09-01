namespace MembershipSystem.Api.Contracts;

public sealed record BranchResponse(Guid Id, string Name);

public sealed record CreateBranchRequest(string Name);
