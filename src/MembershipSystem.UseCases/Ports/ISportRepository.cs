using MembershipSystem.Domain;

namespace MembershipSystem.UseCases.Ports;

public interface ISportRepository
{
    Task<IReadOnlyList<Sport>> ListByBranch(BranchId branchId);
    Task<Sport?> GetById(BranchId branchId, SportId sportId);
}
