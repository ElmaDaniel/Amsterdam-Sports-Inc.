using MembershipSystem.Domain;
using MembershipSystem.UseCases.Ports;

namespace MembershipSystem.Api.Tests.Fakes;

public sealed class FakeSportRepository : ISportRepository
{
    private readonly Dictionary<SportId, Sport> _sports = [];

    public Task<IReadOnlyList<Sport>> ListByBranch(BranchId branchId)
    {
        IReadOnlyList<Sport> result = _sports.Values.Where(s => s.BranchId == branchId).ToList();
        return Task.FromResult(result);
    }

    public Task<Sport?> GetById(BranchId branchId, SportId sportId)
    {
        var found = _sports.GetValueOrDefault(sportId);
        return Task.FromResult(found?.BranchId == branchId ? found : null);
    }

    /// <summary>Test-setup helper — not part of ISportRepository (removed with CreateSport).</summary>
    public Task Add(Sport sport)
    {
        _sports[sport.Id] = sport;
        return Task.CompletedTask;
    }
}
