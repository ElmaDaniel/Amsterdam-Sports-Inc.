using MembershipSystem.Domain;

namespace MembershipSystem.UseCases.Ports;

public interface IMemberRepository
{
    Task<IReadOnlyList<Member>> ListByBranch(BranchId branchId);
    Task<Member?> GetById(BranchId branchId, MemberId memberId);
    Task Add(Member member);
    Task Update(Member member);
    Task Remove(BranchId branchId, MemberId memberId);
}
