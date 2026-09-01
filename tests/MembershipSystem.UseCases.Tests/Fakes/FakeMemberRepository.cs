using MembershipSystem.Domain;
using MembershipSystem.UseCases.Ports;

namespace MembershipSystem.UseCases.Tests.Fakes;

public sealed class FakeMemberRepository : IMemberRepository
{
    private readonly Dictionary<MemberId, Member> _members = [];

    public Task<IReadOnlyList<Member>> ListByBranch(BranchId branchId)
    {
        IReadOnlyList<Member> result = _members.Values
            .Where(m => m.BranchId == branchId)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<Member?> GetById(BranchId branchId, MemberId memberId)
    {
        var found = _members.GetValueOrDefault(memberId);
        return Task.FromResult(found?.BranchId == branchId ? found : null);
    }

    public Task Add(Member member)
    {
        _members[member.Id] = member;
        return Task.CompletedTask;
    }

    public Task Update(Member member)
    {
        _members[member.Id] = member;
        return Task.CompletedTask;
    }

    public Task Remove(BranchId branchId, MemberId memberId)
    {
        if (_members.TryGetValue(memberId, out var existing) && existing.BranchId == branchId)
        {
            _members.Remove(memberId);
        }

        return Task.CompletedTask;
    }
}
