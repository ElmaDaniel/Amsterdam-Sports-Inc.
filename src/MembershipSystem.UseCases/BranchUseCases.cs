using MembershipSystem.Domain;
using MembershipSystem.UseCases.Ports;

namespace MembershipSystem.UseCases;

public sealed class BranchUseCases(IBranchRepository branches)
{
    public async Task<UseCaseResult<IReadOnlyList<BranchSummary>>> ListBranches()
    {
        var found = await branches.ListAll();
        IReadOnlyList<BranchSummary> summaries = found
            .Select(b => new BranchSummary(b.Id.Value, b.Name))
            .ToList();

        return UseCaseResult<IReadOnlyList<BranchSummary>>.Success(summaries);
    }

    public async Task<UseCaseResult<BranchSummary>> CreateBranch(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return UseCaseResult<BranchSummary>.ValidationFailed("Name is required.");
        }

        var branch = new Branch(BranchId.New(), name);
        await branches.Add(branch);

        return UseCaseResult<BranchSummary>.Success(new BranchSummary(branch.Id.Value, branch.Name));
    }
}
