using MembershipSystem.Domain;
using MembershipSystem.UseCases.Ports;

namespace MembershipSystem.Api.Tests.Fakes;

public sealed class FakeBranchRepository : IBranchRepository
{
    private readonly Dictionary<BranchId, Branch> _branches = [];

    public void Seed(Branch branch) => _branches[branch.Id] = branch;

    public Task<Branch?> GetById(BranchId branchId) => Task.FromResult(_branches.GetValueOrDefault(branchId));

    public Task<IReadOnlyList<Branch>> ListAll()
    {
        IReadOnlyList<Branch> result = _branches.Values.ToList();
        return Task.FromResult(result);
    }

    public Task Add(Branch branch)
    {
        _branches[branch.Id] = branch;
        return Task.CompletedTask;
    }
}
