using MembershipSystem.Domain;
using MembershipSystem.UseCases.Ports;

namespace MembershipSystem.UseCases;

public sealed class SportUseCases(ISportRepository sports, IBranchRepository branches)
{
    public async Task<UseCaseResult<IReadOnlyList<SportSummary>>> ListSports(BranchId branchId)
    {
        if (await branches.GetById(branchId) is null)
        {
            return UseCaseResult<IReadOnlyList<SportSummary>>.NotFound();
        }

        var found = await sports.ListByBranch(branchId);
        IReadOnlyList<SportSummary> summaries = found
            .Select(s => new SportSummary(s.Id.Value, s.Name))
            .ToList();

        return UseCaseResult<IReadOnlyList<SportSummary>>.Success(summaries);
    }
}
