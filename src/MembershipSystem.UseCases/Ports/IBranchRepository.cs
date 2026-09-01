using MembershipSystem.Domain;

namespace MembershipSystem.UseCases.Ports;

public interface IBranchRepository
{
    Task<Branch?> GetById(BranchId branchId);
    Task<IReadOnlyList<Branch>> ListAll();
    Task Add(Branch branch);
}
