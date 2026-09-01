using MembershipSystem.Domain;
using MembershipSystem.UseCases.Ports;
using Microsoft.EntityFrameworkCore;

namespace MembershipSystem.Adapters;

public sealed class EfMemberRepository(MembershipDbContext context) : IMemberRepository
{
    public async Task<IReadOnlyList<Member>> ListByBranch(BranchId branchId) =>
        await context.Members.Where(m => m.BranchId == branchId).ToListAsync();

    public Task<Member?> GetById(BranchId branchId, MemberId memberId) =>
        context.Members.FirstOrDefaultAsync(m => m.Id == memberId && m.BranchId == branchId);

    public async Task Add(Member member)
    {
        context.Members.Add(member);
        await context.SaveChangesAsync();
    }

    public async Task Update(Member member)
    {
        context.Members.Update(member);
        await context.SaveChangesAsync();
    }

    public async Task Remove(BranchId branchId, MemberId memberId)
    {
        var existing = await context.Members
            .FirstOrDefaultAsync(m => m.Id == memberId && m.BranchId == branchId);

        if (existing is not null)
        {
            context.Members.Remove(existing);
            await context.SaveChangesAsync();
        }
    }
}
